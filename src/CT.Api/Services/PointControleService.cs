using CT.Api.Mapping;
using CT.Domain.Entities;
using CT.Domain.Exceptions;
using CT.Infrastructure.Data;
using CT.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using D = CT.Domain.Enums;

namespace CT.Api.Services;

public interface IPointControleService
{
    Task<IReadOnlyList<PointControleDto>> ListerAsync(CancellationToken ct);
    Task<(PointControleDto Point, bool Cree)> EnregistrerAsync(PointControleRequete requete, CancellationToken ct);
}

public class PointControleService(CtDbContext db) : IPointControleService
{
    public async Task<IReadOnlyList<PointControleDto>> ListerAsync(CancellationToken ct) =>
        await db.PointsControle
            .OrderBy(p => p.Categorie!.Libelle)
            .ThenByDescending(p => p.Gravite)
            .ThenBy(p => p.Libelle)
            .Select(Projections.PointControle)
            .ToListAsync(ct);

    public async Task<(PointControleDto Point, bool Cree)> EnregistrerAsync(PointControleRequete requete, CancellationToken ct)
    {
        if (!await db.CategoriesPoints.AnyAsync(c => c.Id == requete.CategorieId, ct))
            throw new RegleMetierException("La catégorie indiquée n'existe pas.");

        PointControle point;
        var cree = requete.Id is null;
        if (cree)
        {
            point = new PointControle { Libelle = "" };
            db.PointsControle.Add(point);
        }
        else
        {
            point = await db.PointsControle.FindAsync([requete.Id!.Value], ct)
                ?? throw new IntrouvableException("Point de contrôle introuvable.");
        }

        point.CategorieId = requete.CategorieId;
        point.Libelle = requete.Libelle.Trim();
        point.Gravite = (D.Gravite)requete.Gravite;
        point.Actif = requete.Actif;
        await db.SaveChangesAsync(ct);

        var dto = await db.PointsControle.Where(p => p.Id == point.Id).Select(Projections.PointControle).SingleAsync(ct);
        return (dto, cree);
    }
}
