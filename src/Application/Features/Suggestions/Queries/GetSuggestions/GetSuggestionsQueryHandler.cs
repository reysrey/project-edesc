<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Text;
using KaizenBoard.Application.Common.Interfaces;
using KaizenBoard.Domain.Entities;
using KaizenBoard.Domain.Enums;
using Microsoft.EntityFrameworkCore;
=======
﻿using KaizenBoard.Application.Common.Interfaces;
>>>>>>> 37ce3438b6ee258029c6ecc8ac813269855a51e1

namespace KaizenBoard.Application.Features.Suggestions.Queries.GetSuggestions;

public class GetSuggestionsQueryHandler : IRequestHandler<GetSuggestionsQuery, List<SuggestionDto>>
{
    private readonly IApplicationDbContext _context;
<<<<<<< HEAD
    private readonly IUser _currentUser;

    public GetSuggestionsQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    // B03: серверная фильтрация видимости по ролям и авторству
    public async Task<List<SuggestionDto>> Handle(GetSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var suggestions = await _context.Suggestions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var departments = await _context.Departments
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var roles = _currentUser.Roles ?? new List<string>();

        List<Suggestion> visible = roles.Contains("AdminL1")
            ? suggestions
            : roles.Contains("AdminL2")
                ? suggestions
                    .Where(s => s.Status != SuggestionStatus.Draft &&
                                 departments.Any(d => d.HeadUserId == _currentUser.Id && d.Id == s.TargetDepartmentId))
                    .ToList()
                : suggestions
                    .Where(s => s.CreatedBy == _currentUser.Id || s.Status == SuggestionStatus.Submitted)
                    .ToList();

        return visible
            .Select(s => new SuggestionDto { Id = s.Id, Title = s.Title, Status = s.Status, Created = s.Created })
            .ToList();
=======
    private readonly IUser _user;

    public GetSuggestionsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<List<SuggestionDto>> Handle(
        GetSuggestionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
            throw new UnauthorizedAccessException();

        return await _context.Suggestions
            .AsNoTracking()
            .ApplyVisibility(_user.Id, _user.Roles, _user.DepartmentId)
            .OrderByDescending(s => s.Created)
            .Select(s => new SuggestionDto
            {
                Id = s.Id,
                Title = s.Title,
                Status = s.Status,
                Created = s.Created,
            })
            .ToListAsync(cancellationToken);
>>>>>>> 37ce3438b6ee258029c6ecc8ac813269855a51e1
    }
}
