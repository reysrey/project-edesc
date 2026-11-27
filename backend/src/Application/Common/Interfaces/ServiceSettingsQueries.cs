using KaizenBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KaizenBoard.Application.Common.Interfaces;

public static class ServiceSettingsQueries
{
    public static async Task<ServiceSettings> ReadServiceSettingsAsync(
        this IApplicationDbContext context, CancellationToken cancellationToken) =>
        await context.ServiceSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken) ?? new ServiceSettings();
}
