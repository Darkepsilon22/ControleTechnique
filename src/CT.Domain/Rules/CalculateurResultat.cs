using CT.Domain.Enums;

namespace CT.Domain.Rules;

public record PointEvalue(string Libelle, Gravite Gravite, EtatPoint Etat);

public record ResultatCalcul(ResultatControle Resultat, IReadOnlyList<string> Observations);

public static class CalculateurResultat
{
    public const int DureeValiditeParDefautMois = 12;

    public static ResultatCalcul Calculer(IEnumerable<PointEvalue> points)
    {
        var nonConformes = points.Where(p => p.Etat == EtatPoint.NonConforme).ToList();

        var resultat = nonConformes.Any(p => p.Gravite is Gravite.Majeur or Gravite.Critique)
            ? ResultatControle.Defavorable
            : ResultatControle.Favorable;

        var observations = nonConformes
            .Where(p => p.Gravite == Gravite.Mineur)
            .Select(p => p.Libelle)
            .ToList();

        return new ResultatCalcul(resultat, observations);
    }

    public static DateOnly? CalculerFinValidite(ResultatControle resultat, DateTime dateControle, int dureeValiditeMois)
    {
        if (dureeValiditeMois <= 0)
            throw new ArgumentOutOfRangeException(nameof(dureeValiditeMois), "La durée de validité doit être positive.");

        return resultat == ResultatControle.Favorable
            ? DateOnly.FromDateTime(dateControle).AddMonths(dureeValiditeMois)
            : null;
    }
}
