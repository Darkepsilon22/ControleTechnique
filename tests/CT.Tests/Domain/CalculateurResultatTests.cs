using CT.Domain.Enums;
using CT.Domain.Rules;

namespace CT.Tests.Domain;

public class CalculateurResultatTests
{
    private static PointEvalue Point(Gravite gravite, EtatPoint etat, string libelle = "Point") => new(libelle, gravite, etat);

    [Fact]
    public void Tous_les_points_conformes_donnent_un_resultat_favorable_sans_observation()
    {
        var calcul = CalculateurResultat.Calculer(
        [
            Point(Gravite.Critique, EtatPoint.Conforme),
            Point(Gravite.Majeur, EtatPoint.Conforme),
            Point(Gravite.Mineur, EtatPoint.Conforme)
        ]);

        Assert.Equal(ResultatControle.Favorable, calcul.Resultat);
        Assert.Empty(calcul.Observations);
    }

    [Fact]
    public void Un_point_critique_non_conforme_donne_un_resultat_defavorable()
    {
        var calcul = CalculateurResultat.Calculer(
        [
            Point(Gravite.Critique, EtatPoint.NonConforme),
            Point(Gravite.Mineur, EtatPoint.Conforme)
        ]);

        Assert.Equal(ResultatControle.Defavorable, calcul.Resultat);
    }

    [Fact]
    public void Un_point_majeur_non_conforme_donne_un_resultat_defavorable()
    {
        var calcul = CalculateurResultat.Calculer([Point(Gravite.Majeur, EtatPoint.NonConforme)]);

        Assert.Equal(ResultatControle.Defavorable, calcul.Resultat);
    }

    [Fact]
    public void Un_point_mineur_non_conforme_seul_donne_un_resultat_favorable_avec_observation()
    {
        var calcul = CalculateurResultat.Calculer(
        [
            Point(Gravite.Mineur, EtatPoint.NonConforme, "Essuie-glaces"),
            Point(Gravite.Critique, EtatPoint.Conforme)
        ]);

        Assert.Equal(ResultatControle.Favorable, calcul.Resultat);
        Assert.Equal(["Essuie-glaces"], calcul.Observations);
    }

    [Fact]
    public void Les_points_mineurs_restent_en_observation_meme_si_le_resultat_est_defavorable()
    {
        var calcul = CalculateurResultat.Calculer(
        [
            Point(Gravite.Mineur, EtatPoint.NonConforme, "Clignotants"),
            Point(Gravite.Critique, EtatPoint.NonConforme, "Freinage")
        ]);

        Assert.Equal(ResultatControle.Defavorable, calcul.Resultat);
        Assert.Equal(["Clignotants"], calcul.Observations);
    }

    [Theory]
    [InlineData(Gravite.Mineur)]
    [InlineData(Gravite.Majeur)]
    [InlineData(Gravite.Critique)]
    public void Un_point_non_applicable_n_influence_pas_le_resultat(Gravite gravite)
    {
        var calcul = CalculateurResultat.Calculer([Point(gravite, EtatPoint.NonApplicable)]);

        Assert.Equal(ResultatControle.Favorable, calcul.Resultat);
        Assert.Empty(calcul.Observations);
    }

    [Fact]
    public void La_fin_de_validite_est_de_12_mois_pour_un_resultat_favorable()
    {
        var fin = CalculateurResultat.CalculerFinValidite(ResultatControle.Favorable, new DateTime(2026, 10, 8, 14, 30, 0), 12);

        Assert.Equal(new DateOnly(2027, 10, 8), fin);
    }

    [Fact]
    public void Aucune_fin_de_validite_pour_un_resultat_defavorable()
    {
        var fin = CalculateurResultat.CalculerFinValidite(ResultatControle.Defavorable, new DateTime(2026, 10, 8), 12);

        Assert.Null(fin);
    }

    [Fact]
    public void La_duree_de_validite_est_parametrable()
    {
        var fin = CalculateurResultat.CalculerFinValidite(ResultatControle.Favorable, new DateTime(2026, 1, 15), 24);

        Assert.Equal(new DateOnly(2028, 1, 15), fin);
    }

    [Fact]
    public void Un_controle_du_29_fevrier_expire_le_28_fevrier_suivant()
    {
        var fin = CalculateurResultat.CalculerFinValidite(ResultatControle.Favorable, new DateTime(2028, 2, 29), 12);

        Assert.Equal(new DateOnly(2029, 2, 28), fin);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-6)]
    public void Une_duree_de_validite_nulle_ou_negative_est_refusee(int mois)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CalculateurResultat.CalculerFinValidite(ResultatControle.Favorable, new DateTime(2026, 10, 8), mois));
    }
}
