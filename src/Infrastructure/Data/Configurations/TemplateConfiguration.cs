using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KaizenBoard.Infrastructure.Data.Configurations;

public class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
    }
}
