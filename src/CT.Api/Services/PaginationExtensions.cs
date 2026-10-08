using CT.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CT.Api.Services;

public static class PaginationExtensions
{
    public static async Task<PageResultat<T>> PaginerAsync<T>(this IQueryable<T> requete, int page, int taillePage, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        taillePage = Math.Clamp(taillePage, 1, Pagination.TaillePageMax);

        var total = await requete.CountAsync(ct);
        var elements = await requete.Skip((page - 1) * taillePage).Take(taillePage).ToListAsync(ct);
        return new PageResultat<T>(elements, page, taillePage, total);
    }
}
