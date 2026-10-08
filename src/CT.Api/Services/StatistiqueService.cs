using CT.Infrastructure.Data;
using CT.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using D = CT.Domain.Enums;

namespace CT.Api.Services;

public interface IStatistiqueService
{
    Task<StatistiquesDto> ResumeAsync(DateOnly? du, DateOnly? au, CancellationToken ct);
}

public class StatistiqueService(CtDbContext db, TimeProvider horloge) : IStatistiqueService
{
    public async Task<StatistiquesDto> ResumeAsync(DateOnly? du, DateOnly? au, CancellationToken ct)
    {
        var aujourdhui = DateOnly.FromDateTime(horloge.GetLocalNow().DateTime);
        var fin = au ?? aujourdhui;
        var debut = du ?? new DateOnly(fin.Year, fin.Month, 1).AddMonths(-5);
        if (debut > fin)
            (debut, fin) = (fin, debut);

        var debutPeriode = debut.ToDateTime(TimeOnly.MinValue);
        var finPeriode = fin.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var clotures = db.Controles.Where(c =>
            c.Statut == D.StatutControle.Cloture && c.DateControle >= debutPeriode && c.DateControle < finPeriode);

        var parMois = await clotures
            .GroupBy(c => new { c.DateControle.Year, c.DateControle.Month })
            .Select(g => new ControlesParMoisDto(
                g.Key.Year,
                g.Key.Month,
                g.Count(),
                g.Count(c => c.Resultat == D.ResultatControle.Favorable)))
            .ToListAsync(ct);
        parMois = parMois.OrderBy(m => m.Annee).ThenBy(m => m.Mois).ToList();

        var pointsNonConformes = await db.ResultatsPoints
            .Where(r => r.Etat == D.EtatPoint.NonConforme
                && r.Controle!.Statut == D.StatutControle.Cloture
                && r.Controle.DateControle >= debutPeriode
                && r.Controle.DateControle < finPeriode)
            .GroupBy(r => new { r.PointControle!.Libelle, Categorie = r.PointControle.Categorie!.Libelle })
            .Select(g => new { g.Key.Libelle, g.Key.Categorie, Nombre = g.Count() })
            .OrderByDescending(p => p.Nombre)
            .ThenBy(p => p.Libelle)
            .Take(5)
            .ToListAsync(ct);

        var enCours = await db.Controles.CountAsync(c => c.Statut == D.StatutControle.Brouillon, ct);

        var total = parMois.Sum(m => m.Total);
        var favorables = parMois.Sum(m => m.Favorables);
        var taux = total == 0 ? 0 : Math.Round(100.0 * favorables / total, 1);

        return new StatistiquesDto(debut, fin, total, favorables, total - favorables, taux, enCours, parMois,
            pointsNonConformes.Select(p => new PointNonConformeDto(p.Libelle, p.Categorie, p.Nombre)).ToList());
    }
}
