using MediatR;

namespace KaizenBoard.Application.Features.Templates.Commands.ArchiveTemplate;

public class ArchiveTemplateCommand : IRequest<Guid>
{
    public Guid Id { get; init; }
}
