using CT.Domain.Entities;
using CT.Domain.Enums;
using CT.Domain.Exceptions;

namespace CT.Tests.Domain;

public class ControleTests
{
    private readonly CatalogueDeTest _c = new();

    [Fact]
    public void Un_controle_ouvert_est_en_brouillon()
    {
        var controle = _c.NouveauControle();

        Assert.Equal(StatutControle.Brouillon, controle.Statut);
        Assert.Equal(controle.DateControle, controle.DateControlePeriodique);
    }

    [Fact]
    public void Un_kilometrage_negatif_est_refuse()
    {
        Assert.Throws<RegleMetierException>(() =>
            Controle.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), CatalogueDeTest.DateControle, -1, CatalogueDeTest.DateControle));
    }

    [Fact]
    public void Une_date_de_controle_dans_le_futur_est_refusee()
    {
        Assert.Throws<RegleMetierException>(() =>
            Controle.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), CatalogueDeTest.DateControle.AddDays(3), 1000, CatalogueDeTest.DateControle));
    }

    [Theory]
    [InlineData(Energie.Essence, "8.2.12", "8.2.22")]
    [InlineData(Energie.Diesel, "8.2.22", "8.2.12")]
    public void Les_points_d_emissions_dependent_de_l_energie_du_vehicule(Energie energie, string applicable, string exclu)
    {
        var codes = _c.NouveauControle(energie).PointsASaisir(_c.Points).Select(p => p.Code).ToList();

        Assert.Contains(applicable, codes);
        Assert.DoesNotContain(exclu, codes);
    }

    [Fact]
    public void Un_vehicule_electrique_n_a_aucun_point_d_emissions()
    {
        var codes = _c.NouveauControle(Energie.Electrique).PointsASaisir(_c.Points).Select(p => p.Code).ToList();

        Assert.DoesNotContain(codes, c => c.StartsWith("8.2"));
    }

    [Fact]
    public void Un_point_non_applicable_au_vehicule_ne_peut_pas_etre_saisi()
    {
        var controle = _c.NouveauControle(Energie.Diesel);

        Assert.Throws<RegleMetierException>(() => controle.Saisir(null,
            [new SaisiePoint(_c.EmissionsEssence.Id, EtatPoint.Conforme, [], null)], _c.Points));
    }

    [Fact]
    public void Un_point_avec_defaillance_doit_indiquer_au_moins_une_defaillance()
    {
        var controle = _c.NouveauControle();

        Assert.Throws<RegleMetierException>(() => controle.Saisir(null,
            [new SaisiePoint(_c.Plaquettes.Id, EtatPoint.NonConforme, [], null)], _c.Points));
    }

    [Fact]
    public void Un_point_conforme_ne_peut_pas_comporter_de_defaillance()
    {
        var controle = _c.NouveauControle();
        var defaillance = CatalogueDeTest.D(_c.Plaquettes, "1.1.13.a.1");

        Assert.Throws<RegleMetierException>(() => controle.Saisir(null,
            [new SaisiePoint(_c.Plaquettes.Id, EtatPoint.Conforme, [defaillance.Id], null)], _c.Points));
    }

    [Fact]
    public void Une_defaillance_d_un_autre_point_est_refusee()
    {
        var controle = _c.NouveauControle();
        var autre = CatalogueDeTest.D(_c.FeuxStop, "4.3.1.a.1");

        Assert.Throws<RegleMetierException>(() => controle.Saisir(null,
            [new SaisiePoint(_c.Plaquettes.Id, EtatPoint.NonConforme, [autre.Id], null)], _c.Points));
    }

    [Fact]
    public void Une_nouvelle_saisie_remplace_le_resultat_existant()
    {
        var controle = _c.NouveauControle();
        var usure = CatalogueDeTest.D(_c.Plaquettes, "1.1.13.a.2");
        controle.Saisir(null, [new SaisiePoint(_c.Plaquettes.Id, EtatPoint.Conforme, [], null)], _c.Points);

        controle.Saisir(52_000, [new SaisiePoint(_c.Plaquettes.Id, EtatPoint.NonConforme, [usure.Id], "AV gauche")], _c.Points);

        var resultat = Assert.Single(controle.Resultats);
        Assert.Equal([usure], resultat.Defaillances);
        Assert.Equal("AV gauche", resultat.Commentaire);
        Assert.Equal(52_000, controle.Kilometrage);
    }

    [Fact]
    public void La_cloture_est_refusee_si_des_points_ne_sont_pas_saisis()
    {
        var controle = _c.NouveauControle();
        controle.Saisir(null, [new SaisiePoint(_c.Plaquettes.Id, EtatPoint.Conforme, [], null)], _c.Points);

        var erreur = Assert.Throws<RegleMetierException>(() => controle.Cloturer(_c.Points, CatalogueDeTest.DateControle));
        Assert.Contains("8 point(s)", erreur.Message);
    }

    [Fact]
    public void Des_defaillances_mineures_donnent_un_controle_favorable_valable_deux_ans()
    {
        var controle = _c.ControleCloture(Energie.Essence, CatalogueDeTest.D(_c.EssuieGlace, "3.4.1.b.1"));

        Assert.Equal(ResultatControle.Favorable, controle.Resultat);
        Assert.Equal(new DateOnly(2028, 9, 1), controle.DateFinValidite);
        Assert.Null(controle.DateLimiteContreVisite);
    }

    [Fact]
    public void Une_defaillance_majeure_impose_une_contre_visite_sous_deux_mois()
    {
        var controle = _c.ControleCloture(Energie.Essence, CatalogueDeTest.D(_c.Plaquettes, "1.1.13.a.2"));

        Assert.Equal(ResultatControle.DefavorableMajeur, controle.Resultat);
        Assert.Equal(new DateOnly(2026, 11, 1), controle.DateLimiteContreVisite);
        Assert.Equal(new DateOnly(2026, 11, 1), controle.DateFinValidite);
    }

    [Fact]
    public void Une_defaillance_critique_limite_la_validite_au_jour_du_controle()
    {
        var controle = _c.ControleCloture(Energie.Essence, CatalogueDeTest.D(_c.FreinService, "1.2.2.a.3"));

        Assert.Equal(ResultatControle.DefavorableCritique, controle.Resultat);
        Assert.Equal(new DateOnly(2026, 9, 1), controle.DateFinValidite);
        Assert.Equal(new DateOnly(2026, 11, 1), controle.DateLimiteContreVisite);
    }

    [Fact]
    public void Un_controle_cloture_refuse_toute_nouvelle_saisie()
    {
        var controle = _c.ControleCloture(Energie.Essence);

        Assert.Throws<ControleClotureException>(() => controle.Saisir(60_000,
            [new SaisiePoint(_c.Plaquettes.Id, EtatPoint.Conforme, [], null)], _c.Points));
    }

    [Fact]
    public void Un_controle_ne_peut_pas_etre_cloture_deux_fois()
    {
        var controle = _c.ControleCloture(Energie.Essence);

        Assert.Throws<ControleClotureException>(() => controle.Cloturer(_c.Points, CatalogueDeTest.DateControle));
    }
}
