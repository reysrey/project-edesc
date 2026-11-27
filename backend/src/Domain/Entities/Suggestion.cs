using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using KaizenBoard.Domain.Enums;

namespace KaizenBoard.Domain.Entities;

public class Suggestion: BaseAuditableEntity
{
    public string? RegistrationNumber { get; set; }
    public Guid? TemplateId { get; set; }
    public string? CreationKey { get; init; }
    public string? CreationPayloadHash { get; init; }
    public Guid TargetDepartmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImprovementObject { get; set; } = string.Empty;
    public string CurrentFlaws { get; set; } = string.Empty;
    public string ProposedSolution  { get; set; } = string.Empty;
    public string ExpectedResultBefore  { get; set; } = string.Empty;
    public string ExpectedResultAfter  { get; set; } = string.Empty;
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Draft;
    public int SubmissionCount { get; private set; }
    public bool IsAnonymous { get; set; }
    public ICollection<SuggestionSupport> Supports { get; } = new List<SuggestionSupport>();
    public ICollection<SuggestionActivity> Activity { get; } = new List<SuggestionActivity>();
    public DateTime? StatusDeadlineAt { get; set; }
    public string? CommissionNote { get; set; } = string.Empty;
    public Guid? HelperDepartmentId { get; set; }
    public string? ImplementerUserId { get; set; }
    public DateTime? ImplementationDeadline  { get; set; }
    public bool HasEconomicEffect { get; set; }
    public decimal? ImplementationCost { get; set; }
    public decimal? EconomicEffectAmount { get; set; }
    public decimal? RewardAmount { get; set; }

    public static bool CanEdit(SuggestionStatus status) => CanSubmit(status);

    public static bool CanSubmit(SuggestionStatus status) =>
        status is SuggestionStatus.Draft or SuggestionStatus.ReturnedByManager or SuggestionStatus.RevisionRequired;

    public static bool CanBeModerated(SuggestionStatus status) =>
        status == SuggestionStatus.Submitted;

    public void Submit()
    {
        if (!CanSubmit(Status))
            throw new InvalidOperationException("Отправить можно только черновик или предложение после доработки");

        Status = SuggestionStatus.Submitted;
        StatusDeadlineAt = DateTime.UtcNow.AddDays(1);
        SubmissionCount++;
    }
    public void ApproveByManager(string regNumber)
    {
        if (!CanBeModerated(Status))
            throw new InvalidOperationException("Одобрить можно только заявку, которая находится на проверке у руководителя.");
        if (string.IsNullOrWhiteSpace(regNumber))
            throw new ArgumentException("Регистрационный номер не может быть пустым.", nameof(regNumber));
        if (RegistrationNumber is not null && RegistrationNumber != regNumber)
            throw new InvalidOperationException("При повторной передаче сохраняется исходный регистрационный номер.");

        Status = SuggestionStatus.InCommission;
        RegistrationNumber = regNumber;
        StatusDeadlineAt = DateTime.UtcNow.AddDays(14);
         
           
    }

    public void ReturnByManager(string reason)
    {
        if (!CanBeModerated(Status))
            throw new InvalidOperationException("Вернуть на доработку можно только заявку на проверке.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Необходимо указать причину возврата.", nameof(reason));

        Status = SuggestionStatus.ReturnedByManager;
        StatusDeadlineAt = null;
        CommissionNote = reason;
    }

    public void RejectByCommission(string reason)
    {
        if (Status != SuggestionStatus.InCommission)
            throw new InvalidOperationException("Отклонить можно только заявку, которая находится в комиссии.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Необходимо указать причину отклонения.", nameof(reason));

        Status = SuggestionStatus.Rejected;
        StatusDeadlineAt = null;
        CommissionNote = reason;
    }

    public void ApproveByCommission(string? note)
    {
        if (Status != SuggestionStatus.InCommission)
            throw new InvalidOperationException("Одобрить можно только заявку, которая находится в комиссии.");

        Status = SuggestionStatus.ApprovedPlanning;
        StatusDeadlineAt = null;
        CommissionNote = note;
    }

    public void ReturnByCommission(string reason)
    {
        if (Status != SuggestionStatus.InCommission)
            throw new InvalidOperationException("Вернуть можно только заявку, которая находится в комиссии.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = SuggestionStatus.RevisionRequired;
        StatusDeadlineAt = null;
        CommissionNote = reason;
    }

    public void RecordActivity(SuggestionStatus previous, string actorId, string? note, bool notificationsEnabled)
    {
        Activity.Add(new SuggestionActivity
        {
            SuggestionId = Id,
            ActorId = actorId,
            OccurredAt = DateTimeOffset.UtcNow,
            FromStatus = previous,
            ToStatus = Status,
            Note = note,
            NotifyAuthor = notificationsEnabled && actorId != CreatedBy
        });
    }
}
