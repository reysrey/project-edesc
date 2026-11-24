using KaizenBoard.Application.Common.Security;
using KaizenBoard.Domain.Constants;
using MediatR;

namespace KaizenBoard.Application.Features.Templates.Commands.ArchiveTemplate;

[Authorize(Roles = Roles.AdminL1)]
public class ArchiveTemplateCommand : IRequest<Guid>
{
    public Guid Id { get; init; }
}
