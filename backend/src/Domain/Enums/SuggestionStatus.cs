using System;
using System.Collections.Generic;
using System.Text;

namespace KaizenBoard.Domain.Enums;

public enum SuggestionStatus
{
    Draft,
    Submitted,
    ReturnedByManager,
    InCommission,
    RevisionRequired,
    Rejected,
    ApprovedPlanning,
    InImplementation,
    Implemented
}
