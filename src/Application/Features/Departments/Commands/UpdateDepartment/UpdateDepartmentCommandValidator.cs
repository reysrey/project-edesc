using FluentValidation;
using KaizenBoard.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Features.Departments.Commands.UpdateDepartment;

public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateDepartmentCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Название цеха обязательно для заполнения.")
            .MaximumLength(200).WithMessage("Название не должно превышать 200 символов.")
            .MustAsync(BeUniqueName).WithMessage("Цех с таким названием уже существует.");

        RuleFor(v => v.EmployeesCount)
            .GreaterThanOrEqualTo(0).WithMessage("Количество сотрудников не может быть отрицательным.");
    }

    private async Task<bool> BeUniqueName(UpdateDepartmentCommand command, string name, CancellationToken cancellationToken)
    {
        return !await _context.Departments
            .AnyAsync(d => d.Name == name && d.Id != command.Id, cancellationToken);
    }
}
