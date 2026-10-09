using CT.Api.Mapping;
using CT.Domain.Entities;
using CT.Domain.Exceptions;
using CT.Infrastructure.Data;
using CT.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CT.Api.Services;

public interface IPointControleService
{
    Task<IReadOnlyList<PointControleDto>> ListerAsync(CancellationToken ct);
    Task<(PointControleDto Point, bool Cree)> EnregistrerPointAsync(PointControleRequete requete, CancellationToken ct);
    Task<(PointControleDto Point, bool Cree)> EnregistrerDefaillanceAsync(Guid pointId, DefaillanceRequete requete, CancellationToken ct);
}

public class PointControleService(CtDbContext db) : IPointControleService
{
    public async Task<IReadOnlyList<PointControleDto>> ListerAsync(CancellationToken ct)
    {
        var points = await Catalogue().ToListAsync(ct);
        return points.OrderBy(p => Projections.CleTri(p.Code)).Select(Projections.VersDto).ToList();
    }

    public async Task<(PointControleDto Point, bool Cree)> EnregistrerPointAsync(PointControleRequete requete, CancellationToken ct)
    {
        var code = requete.Code.Trim();
        PointControle.VerifierCode(code);
        var fonction = await db.Fonctions.FindAsync([requete.FonctionId], ct)
            ?? throw new RegleMetierException("La fonction indiquée n'existe pas.");
        if (!code.StartsWith(fonction.Numero + "."))
            throw new RegleMetierException($"Le code d'un point de la fonction {fonction.Numero} doit commencer par « {fonction.Numero}. ».");
        if (await db.PointsControle.AnyAsync(p => p.Code == code && p.Id != requete.Id, ct))
            throw new ConflitException($"Le point {code} existe déjà.");

        var cree = requete.Id is null;
        PointControle point;
        if (cree)
        {
            point = new PointControle { Code = code, Libelle = "" };
            db.PointsControle.Add(point);
        }
        else
        {
            point = await db.PointsControle.FindAsync([requete.Id!.Value], ct)
                ?? throw new IntrouvableException("Point de contrôle introuvable.");
            if (point.Code != code && await db.Defaillances.AnyAsync(d => d.PointControleId == point.Id, ct))
                throw new RegleMetierException("Le code d'un point qui a des défaillances ne peut pas être modifié.");
        }

        point.Code = code;
        point.FonctionId = fonction.Id;
        point.Libelle = requete.Libelle.Trim();
        point.Actif = requete.Actif;
        await db.SaveChangesAsync(ct);

        return (await ObtenirAsync(point.Id, ct), cree);
    }

    public async Task<(PointControleDto Point, bool Cree)> EnregistrerDefaillanceAsync(Guid pointId, DefaillanceRequete requete, CancellationToken ct)
    {
        var point = await db.PointsControle.FindAsync([pointId], ct)
            ?? throw new IntrouvableException("Point de contrôle introuvable.");
        var code = requete.Code.Trim();
        var niveau = Defaillance.NiveauDepuisCode(code, point.Code);
        if (await db.Defaillances.AnyAsync(d => d.Code == code && d.Id != requete.Id, ct))
            throw new ConflitException($"La défaillance {code} existe déjà.");

        var cree = requete.Id is null;
        Defaillance defaillance;
        if (cree)
        {
            defaillance = new Defaillance { PointControleId = point.Id, Code = code, Libelle = "" };
            db.Defaillances.Add(defaillance);
        }
        else
        {
            defaillance = await db.Defaillances.SingleOrDefaultAsync(d => d.Id == requete.Id && d.PointControleId == pointId, ct)
                ?? throw new IntrouvableException("Défaillance introuvable.");
        }

        defaillance.Code = code;
        defaillance.Niveau = niveau;
        defaillance.Libelle = requete.Libelle.Trim();
        defaillance.Actif = requete.Actif;
        await db.SaveChangesAsync(ct);

        return (await ObtenirAsync(point.Id, ct), cree);
    }

    private IQueryable<PointControle> Catalogue() =>
        db.PointsControle.Include(p => p.Fonction).Include(p => p.Defaillances).AsSplitQuery();

    private async Task<PointControleDto> ObtenirAsync(Guid id, CancellationToken ct) =>
        Projections.VersDto(await Catalogue().SingleAsync(p => p.Id == id, ct));
}
