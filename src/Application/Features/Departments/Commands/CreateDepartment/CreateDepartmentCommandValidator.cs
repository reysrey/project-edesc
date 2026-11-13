using FluentValidation;

namespace KaizenBoard.Application.Features.Departments.Commands.CreateDepartment;

public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(v => v.Name)
            .MaximumLength(200).WithMessage("Название не должно превышать 200 символов.")
            .NotEmpty().WithMessage("Название обязательно");

        RuleFor(v => v.EmployeesCount)
            .GreaterThanOrEqualTo(0).WithMessage("Количество сотрудников не должно быть отрицательным.");
    }
}
