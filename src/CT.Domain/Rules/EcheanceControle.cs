using CT.Domain.Enums;

namespace CT.Domain.Rules;

public record Echeance(DateOnly DateLimite, StatutEcheance Statut);

public static class EcheanceControle
{
    public static Echeance Calculer(DateOnly datePremiereImmatriculation, ResultatControle? dernierResultat,
        DateOnly? finValidite, DateOnly? limiteContreVisite, DateOnly aujourdhui)
    {
        switch (dernierResultat)
        {
            case null:
                var premier = Reglementation.LimitePremierControle(datePremiereImmatriculation);
                return new Echeance(premier, aujourdhui > premier ? StatutEcheance.ControleEnRetard : StatutEcheance.AJour);

            case ResultatControle.Favorable:
                var limite = finValidite!.Value;
                return new Echeance(limite, aujourdhui > limite ? StatutEcheance.ControleEnRetard : StatutEcheance.AJour);

            case ResultatControle.DefavorableMajeur:
                return aujourdhui > limiteContreVisite!.Value
                    ? new Echeance(limiteContreVisite.Value, StatutEcheance.ControleEnRetard)
                    : new Echeance(limiteContreVisite.Value, StatutEcheance.ContreVisiteAFaire);

            default:
                return aujourdhui > finValidite!.Value
                    ? new Echeance(limiteContreVisite!.Value, StatutEcheance.CirculationInterdite)
                    : new Echeance(limiteContreVisite!.Value, StatutEcheance.ContreVisiteAFaire);
        }
    }
}
