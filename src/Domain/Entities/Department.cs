using System;
using System.Collections.Generic;
using System.Text;

namespace KaizenBoard.Domain.Entities;

public class Department: BaseAuditableEntity // в нем уже есть Id 
{
    public string Name { get; set; } = string.Empty;
    public Guid? HeadUserId { get; set; }
    public int EmployeesCount { get; set; }
}
