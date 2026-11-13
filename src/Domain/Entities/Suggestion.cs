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
    public Guid TargetDepartmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImprovementObject { get; set; } = string.Empty;
    public string CurrentFlaws { get; set; } = string.Empty;
    public string ProposedSolution  { get; set; } = string.Empty;
    public string ExpectedResultBefore  { get; set; } = string.Empty;
    public string ExpectedResultAfter  { get; set; } = string.Empty;
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Draft;
    public DateTime? StatusDeadlineAt { get; set; }
    public string? CommissionNote { get; set; } = string.Empty;
    public Guid? HelperDepartmentId { get; set; }
    public string? ImplementerUserId { get; set; }
    public DateTime? ImplementationDeadline  { get; set; }
    public bool HasEconomicEffect { get; set; }
    public decimal? ImplementationCost { get; set; }
    public decimal? EconomicEffectAmount { get; set; }
    public decimal? RewardAmount { get; set; }

    public void Submit()
    {
        if (Status != SuggestionStatus.Draft && Status != SuggestionStatus.ReturnedByManager)
            throw new InvalidOperationException("Отправить можно только черновик или предложение после доработки");

        Status = SuggestionStatus.Submitted;
        StatusDeadlineAt = DateTime.UtcNow.AddDays(1);
    }
    public void ApproveByManager(string regNumber)
    {
        if (Status != SuggestionStatus.Submitted)
            throw new InvalidOperationException("Одобрить можно только заявку, которая находится на проверке у руководителя.");
        if (string.IsNullOrWhiteSpace(regNumber))
            throw new ArgumentException("Регистрационный номер не может быть пустым.", nameof(regNumber));

        Status = SuggestionStatus.InCommission;
        RegistrationNumber = regNumber;
        StatusDeadlineAt = DateTime.UtcNow.AddDays(14);
         
           
    }

    public void ReturnByManager(string reason)
    {
        if (Status != SuggestionStatus.Submitted)
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
        CommissionNote = reason;
    }
}
