using CT.Domain.Enums;

namespace CT.Domain.Rules;

// Arrêté du 18 juin 1991 modifié : articles 4 (validité) et 7 (résultat), code de la route (premier contrôle).
public static class Reglementation
{
    public const int ValiditeControleFavorableAns = 2;
    public const int DelaiContreVisiteMois = 2;
    public const int AgePremierControleAns = 4;

    public static ResultatControle Resultat(IEnumerable<NiveauDefaillance> niveaux)
    {
        var liste = niveaux.ToList();
        if (liste.Contains(NiveauDefaillance.Critique))
            return ResultatControle.DefavorableCritique;
        if (liste.Contains(NiveauDefaillance.Majeure))
            return ResultatControle.DefavorableMajeur;
        return ResultatControle.Favorable;
    }

    public static DateOnly FinValidite(ResultatControle resultat, DateOnly dateControle, DateOnly dateControlePeriodique) =>
        resultat switch
        {
            ResultatControle.Favorable => dateControlePeriodique.AddYears(ValiditeControleFavorableAns),
            ResultatControle.DefavorableMajeur => dateControlePeriodique.AddMonths(DelaiContreVisiteMois),
            _ => dateControle
        };

    public static DateOnly LimiteContreVisite(DateOnly dateControlePeriodique) =>
        dateControlePeriodique.AddMonths(DelaiContreVisiteMois);

    public static DateOnly LimitePremierControle(DateOnly datePremiereImmatriculation) =>
        datePremiereImmatriculation.AddYears(AgePremierControleAns);
}
