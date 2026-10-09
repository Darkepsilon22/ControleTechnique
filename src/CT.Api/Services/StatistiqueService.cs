using CT.Infrastructure.Data;
using CT.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using D = CT.Domain.Enums;
using S = CT.Shared.Enums;

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
        var periodiques = clotures.Where(c => c.ControleInitialId == null);

        var parMois = await periodiques
            .GroupBy(c => new { c.DateControle.Year, c.DateControle.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Total = g.Count(),
                Favorables = g.Count(c => c.Resultat == D.ResultatControle.Favorable),
                Majeurs = g.Count(c => c.Resultat == D.ResultatControle.DefavorableMajeur)
            })
            .ToListAsync(ct);

        var defaillances = await periodiques
            .SelectMany(c => c.Resultats)
            .SelectMany(r => r.Defaillances)
            .GroupBy(d => new { d.Code, d.Libelle, d.Niveau })
            .Select(g => new { g.Key.Code, g.Key.Libelle, g.Key.Niveau, Nombre = g.Count() })
            .OrderByDescending(d => d.Nombre)
            .ThenBy(d => d.Code)
            .Take(6)
            .ToListAsync(ct);

        var contreVisites = await clotures.CountAsync(c => c.ControleInitialId != null, ct);
        var enCours = await db.Controles.CountAsync(c => c.Statut == D.StatutControle.Brouillon, ct);

        var total = parMois.Sum(m => m.Total);
        var favorables = parMois.Sum(m => m.Favorables);
        var majeurs = parMois.Sum(m => m.Majeurs);
        var taux = total == 0 ? 0 : Math.Round(100.0 * favorables / total, 1);

        return new StatistiquesDto(debut, fin, total, favorables, majeurs, total - favorables - majeurs, taux, contreVisites, enCours,
            parMois.OrderBy(m => m.Year).ThenBy(m => m.Month)
                .Select(m => new ControlesParMoisDto(m.Year, m.Month, m.Total, m.Favorables)).ToList(),
            defaillances.Select(d => new DefaillanceFrequenteDto(d.Code, d.Libelle, (S.NiveauDefaillance)d.Niveau, d.Nombre)).ToList());
    }
}
