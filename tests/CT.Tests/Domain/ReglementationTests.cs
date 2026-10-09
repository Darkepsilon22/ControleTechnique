using CT.Domain.Entities;
using CT.Domain.Enums;
using CT.Domain.Exceptions;
using CT.Domain.Rules;

namespace CT.Tests.Domain;

public class ReglementationTests
{
    private static readonly DateOnly Jour = new(2026, 9, 1);

    [Fact]
    public void Aucune_defaillance_donne_un_resultat_favorable()
    {
        Assert.Equal(ResultatControle.Favorable, Reglementation.Resultat([]));
    }

    [Fact]
    public void Des_defaillances_mineures_seules_donnent_un_resultat_favorable()
    {
        Assert.Equal(ResultatControle.Favorable, Reglementation.Resultat([NiveauDefaillance.Mineure, NiveauDefaillance.Mineure]));
    }

    [Fact]
    public void Une_defaillance_majeure_donne_un_resultat_S()
    {
        Assert.Equal(ResultatControle.DefavorableMajeur, Reglementation.Resultat([NiveauDefaillance.Mineure, NiveauDefaillance.Majeure]));
    }

    [Fact]
    public void Une_defaillance_critique_donne_un_resultat_R_meme_avec_des_majeures()
    {
        Assert.Equal(ResultatControle.DefavorableCritique,
            Reglementation.Resultat([NiveauDefaillance.Majeure, NiveauDefaillance.Critique]));
    }

    [Fact]
    public void Un_resultat_favorable_est_valide_deux_ans()
    {
        Assert.Equal(new DateOnly(2028, 9, 1), Reglementation.FinValidite(ResultatControle.Favorable, Jour, Jour));
    }

    [Fact]
    public void Un_resultat_S_est_valide_deux_mois_apres_le_controle_periodique()
    {
        Assert.Equal(new DateOnly(2026, 11, 1), Reglementation.FinValidite(ResultatControle.DefavorableMajeur, Jour.AddDays(10), Jour));
    }

    [Fact]
    public void Un_resultat_R_n_est_valide_que_le_jour_du_controle()
    {
        Assert.Equal(Jour.AddDays(10), Reglementation.FinValidite(ResultatControle.DefavorableCritique, Jour.AddDays(10), Jour));
    }

    [Fact]
    public void Une_contre_visite_favorable_est_valide_deux_ans_depuis_le_controle_periodique()
    {
        Assert.Equal(new DateOnly(2028, 9, 1), Reglementation.FinValidite(ResultatControle.Favorable, Jour.AddDays(25), Jour));
    }

    [Fact]
    public void Le_premier_controle_intervient_au_quatrieme_anniversaire_de_la_premiere_immatriculation()
    {
        Assert.Equal(new DateOnly(2030, 3, 15), Reglementation.LimitePremierControle(new DateOnly(2026, 3, 15)));
    }

    [Theory]
    [InlineData("1.1.13.a.1", NiveauDefaillance.Mineure)]
    [InlineData("1.1.13.a.2", NiveauDefaillance.Majeure)]
    [InlineData("1.1.13.a.3", NiveauDefaillance.Critique)]
    public void Le_niveau_d_une_defaillance_est_donne_par_le_dernier_chiffre_du_code(string code, NiveauDefaillance attendu)
    {
        Assert.Equal(attendu, Defaillance.NiveauDepuisCode(code, "1.1.13"));
    }

    [Theory]
    [InlineData("1.1.13.a.4")]
    [InlineData("1.1.14.a.2")]
    [InlineData("1.1.13.2")]
    public void Un_code_de_defaillance_invalide_ou_d_un_autre_point_est_refuse(string code)
    {
        Assert.Throws<RegleMetierException>(() => Defaillance.NiveauDepuisCode(code, "1.1.13"));
    }
}
