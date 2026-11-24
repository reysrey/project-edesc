using MediatR;

namespace KaizenBoard.Application.Features.Templates.Queries.GetTemplates;

public class GetTemplatesQuery : IRequest<List<TemplateDto>>
{
}
