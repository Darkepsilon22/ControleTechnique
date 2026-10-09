using CT.Domain.Enums;
using CT.Domain.Rules;

namespace CT.Tests.Domain;

public class EcheanceControleTests
{
    private static readonly DateOnly PremiereImmatriculation = new(2022, 3, 10);

    [Fact]
    public void Un_vehicule_jamais_controle_doit_l_etre_avant_ses_quatre_ans()
    {
        var echeance = EcheanceControle.Calculer(PremiereImmatriculation, null, null, null, new DateOnly(2025, 1, 1));

        Assert.Equal(new Echeance(new DateOnly(2026, 3, 10), StatutEcheance.AJour), echeance);
    }

    [Fact]
    public void Un_vehicule_de_plus_de_quatre_ans_jamais_controle_est_en_retard()
    {
        var echeance = EcheanceControle.Calculer(PremiereImmatriculation, null, null, null, new DateOnly(2026, 4, 1));

        Assert.Equal(StatutEcheance.ControleEnRetard, echeance.Statut);
    }

    [Fact]
    public void Apres_un_controle_favorable_l_echeance_est_la_fin_de_validite()
    {
        var echeance = EcheanceControle.Calculer(PremiereImmatriculation, ResultatControle.Favorable,
            new DateOnly(2028, 9, 1), null, new DateOnly(2026, 10, 1));

        Assert.Equal(new Echeance(new DateOnly(2028, 9, 1), StatutEcheance.AJour), echeance);
    }

    [Fact]
    public void Apres_un_resultat_S_une_contre_visite_est_a_faire()
    {
        var echeance = EcheanceControle.Calculer(PremiereImmatriculation, ResultatControle.DefavorableMajeur,
            new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 1), new DateOnly(2026, 10, 1));

        Assert.Equal(new Echeance(new DateOnly(2026, 11, 1), StatutEcheance.ContreVisiteAFaire), echeance);
    }

    [Fact]
    public void Apres_un_resultat_R_la_circulation_est_interdite_des_le_lendemain()
    {
        var echeance = EcheanceControle.Calculer(PremiereImmatriculation, ResultatControle.DefavorableCritique,
            new DateOnly(2026, 9, 1), new DateOnly(2026, 11, 1), new DateOnly(2026, 9, 2));

        Assert.Equal(StatutEcheance.CirculationInterdite, echeance.Statut);
    }

    [Fact]
    public void Sans_contre_visite_dans_les_deux_mois_un_controle_complet_est_necessaire()
    {
        var echeance = EcheanceControle.Calculer(PremiereImmatriculation, ResultatControle.DefavorableMajeur,
            new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2));

        Assert.Equal(StatutEcheance.ControleEnRetard, echeance.Statut);
    }
}
