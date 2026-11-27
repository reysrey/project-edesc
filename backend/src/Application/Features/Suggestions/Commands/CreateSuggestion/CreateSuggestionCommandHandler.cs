using FluentValidation.Results;
using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;

using ValidationException = KaizenBoard.Application.Common.Exceptions.ValidationException;

namespace KaizenBoard.Application.Features.Suggestions.Commands.CreateSuggestion;

public class CreateSuggestionCommandHandler : IRequestHandler<CreateSuggestionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IValidator<CreateSuggestionInput> _inputValidator;

    public CreateSuggestionCommandHandler(
        IApplicationDbContext context, IUser user, IValidator<CreateSuggestionInput> inputValidator)
    {
        _context = context;
        _user = user;
        _inputValidator = inputValidator;
    }

    public async Task<Guid> Handle(CreateSuggestionCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id;
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException();

        var input = request.Input;
        var key = request.IdempotencyKey;
        string? payloadHash = null;
        if (key is not null)
        {
            payloadHash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(input)));
            var existing = await FindCreatedSuggestion(userId, key, cancellationToken);
            if (existing is not null)
                return GetReplayId(existing, payloadHash);
        }

        var validation = await _inputValidator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // Неизвестное подразделение — понятная ошибка поля, а не сбой БД.
        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == input.TargetDepartmentId, cancellationToken);

        if (!departmentExists)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("TargetDepartmentId", "Указанное подразделение не найдено.")
            });
        }

        // Привязанный шаблон должен существовать и быть активным.
        if (input.TemplateId is { } templateId)
        {
            var templateAvailable = await _context.Templates
                .AnyAsync(t => t.Id == templateId && t.IsActive, cancellationToken);

            if (!templateAvailable)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure("TemplateId", "Указанный шаблон не найден или недоступен.")
                });
            }
        }

        var entity = new Suggestion
        {
            Title = input.Title,
            CurrentFlaws = input.CurrentFlaws,
            ProposedSolution = input.ProposedSolution,
            ImprovementObject = input.ImprovementObject,
            TargetDepartmentId = input.TargetDepartmentId,
            TemplateId = input.TemplateId,
            CreationKey = key,
            CreationPayloadHash = payloadHash,
            Status = SuggestionStatus.Draft
        };

        _context.Suggestions.Add(entity);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (key is not null)
        {
            // Уникальный индекс допускает только одну запись при параллельных запросах.
            _context.Suggestions.Entry(entity).State = EntityState.Detached;
            var existing = await FindCreatedSuggestion(userId, key, cancellationToken);
            if (existing is null)
                throw;

            return GetReplayId(existing, payloadHash!);
        }

        return entity.Id;
    }

    private Task<Suggestion?> FindCreatedSuggestion(string userId, string key, CancellationToken cancellationToken) =>
        _context.Suggestions.AsNoTracking()
            .SingleOrDefaultAsync(s => s.CreatedBy == userId && s.CreationKey == key, cancellationToken);

    private static Guid GetReplayId(Suggestion existing, string payloadHash)
    {
        if (!string.Equals(existing.CreationPayloadHash, payloadHash, StringComparison.Ordinal))
            throw new InvalidOperationException("Этот ключ идемпотентности уже использован для другого предложения.");

        return existing.Id;
    }
}
