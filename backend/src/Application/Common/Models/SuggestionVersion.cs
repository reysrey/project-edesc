using System.Security.Cryptography;
using System.Text;
using KaizenBoard.Domain.Entities;

namespace KaizenBoard.Application.Common.Models;

/// <summary>
/// B01/B02: непрозрачный маркер конкурентного изменения предложения.
/// Считается из полей, которые меняются при редактировании и переводе статуса,
/// поэтому любое изменение записи даёт другой маркер: клиент передаёт его
/// обратно в PUT, а сервер отвечает 409, если запись уже изменена кем-то другим.
/// </summary>
public static class SuggestionVersion
{
    public static string Compute(Suggestion suggestion)
    {
        // Интерполяция вместо string.Join: null-поля дают пустую строку,
        // формат O — стабильный round-trip для DateTimeOffset.
        var payload =
            $"{suggestion.Id}|{suggestion.Created:O}|{suggestion.LastModified:O}|{(int)suggestion.Status}|" +
            $"{suggestion.Title}|{suggestion.CurrentFlaws}|{suggestion.ProposedSolution}|" +
            $"{suggestion.ImprovementObject}|{suggestion.TargetDepartmentId}|" +
            $"{suggestion.TemplateId}|{suggestion.RegistrationNumber}";

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
