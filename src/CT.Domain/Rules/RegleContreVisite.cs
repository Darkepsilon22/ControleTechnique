using CT.Domain.Entities;
using CT.Domain.Enums;

namespace CT.Domain.Rules;

// Annexe I, section F : points à contrôler lors d'une contre-visite.
public static class RegleContreVisite
{
    public static IReadOnlySet<string> PointsAReverifier(IEnumerable<string> codesDefaillancesMajeuresOuCritiques, IReadOnlyCollection<string> codesPoints)
    {
        var defaillances = codesDefaillancesMajeuresOuCritiques.ToList();
        if (defaillances.Any(c => Defaillance.CodePointDe(c) == "0.2.1" || c == "0.4.1.a.2"))
            return codesPoints.ToHashSet();

        var aReverifier = codesPoints.Where(p => p.StartsWith("0.") || p == "7.11.1").ToHashSet();

        foreach (var point in defaillances.Select(Defaillance.CodePointDe).Distinct())
        {
            var ensemble = PointControle.EnsembleDe(point);
            var fonction = point.Split('.')[0];

            IEnumerable<string> ajouts = (fonction, ensemble, point) switch
            {
                ("1" or "2", _, _) => codesPoints.Where(p => p.StartsWith(fonction + ".")),
                (_, "5.1" or "5.3", _) => DansEnsembles(codesPoints, "5.1", "5.3"),
                (_, _, "6.2.5" or "6.2.6") => DansEnsembles(codesPoints, "7.1").Append("6.2.5").Append("6.2.6"),
                (_, "8.1" or "8.2", _) or (_, _, "6.1.2" or "6.1.3") =>
                    DansEnsembles(codesPoints, "8.1", "8.2").Append("6.1.2").Append("6.1.3"),
                ("6", _, _) => [point],
                _ => DansEnsembles(codesPoints, ensemble)
            };

            aReverifier.UnionWith(ajouts.Where(codesPoints.Contains));
        }

        return aReverifier;
    }

    public static IEnumerable<string> CodesMotivantUneContreVisite(Controle controle) =>
        controle.Resultats
            .SelectMany(r => r.Defaillances)
            .Where(d => d.Niveau is NiveauDefaillance.Majeure or NiveauDefaillance.Critique)
            .Select(d => d.Code);

    private static IEnumerable<string> DansEnsembles(IEnumerable<string> codesPoints, params string[] ensembles) =>
        codesPoints.Where(p => ensembles.Contains(PointControle.EnsembleDe(p)));
}
