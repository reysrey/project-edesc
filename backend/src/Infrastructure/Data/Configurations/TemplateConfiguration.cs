using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KaizenBoard.Infrastructure.Data.Configurations;

public class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.HasData(
            Preset("da28c801-5d1f-4dfa-88ed-028b00eb0101", "Модернизация",
                "Оборудование или инструмент", "Какое оборудование или инструмент мешает работе?",
                "Что нужно изменить или заменить?"),
            Preset("da28c801-5d1f-4dfa-88ed-028b00eb0102", "Сокращение времени",
                "Рабочий процесс", "На каком шаге теряется время?",
                "Какие шаги можно убрать или упростить?"),
            Preset("da28c801-5d1f-4dfa-88ed-028b00eb0103", "Безопасность",
                "Безопасность", "Где возникает риск и кто с ним сталкивается?",
                "Как устранить риск или снизить его?"),
            Preset("da28c801-5d1f-4dfa-88ed-028b00eb0104", "Качество",
                "Операция контроля качества", "В каком месте процесса возникает дефект?",
                "Как предупредить или раньше обнаружить дефект?"),
            Preset("da28c801-5d1f-4dfa-88ed-028b00eb0105", "Экономия ресурсов",
                "Расход материалов или энергии", "Где возникает лишний расход?",
                "Как уменьшить расход без потери качества?"),
            Preset("da28c801-5d1f-4dfa-88ed-028b00eb0106", "Другое",
                "Рабочее место или процесс", "Что сейчас мешает выполнить работу?",
                "Какое изменение поможет?"));
    }

    private static Template Preset(string id, string title, string improvementObject, string problemHint, string solutionHint) =>
        new()
        {
            Id = Guid.Parse(id), Title = title, Type = TemplateType.Template,
            ImprovementObject = improvementObject, ProblemHint = problemHint, SolutionHint = solutionHint,
            IsActive = true, Created = DateTimeOffset.UnixEpoch, LastModified = DateTimeOffset.UnixEpoch
        };
}
