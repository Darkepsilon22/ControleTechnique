using CT.Domain.Entities;
using CT.Domain.Enums;
using CT.Domain.Exceptions;

namespace CT.Tests.Domain;

public class ContreVisiteTests
{
    private readonly CatalogueDeTest _c = new();

    private static Controle ContreVisite(Controle precedent, int joursApresLePeriodique = 20)
    {
        var date = CatalogueDeTest.DateControle.AddDays(joursApresLePeriodique);
        var contreVisite = Controle.OuvrirContreVisite(precedent, Guid.NewGuid(), date, 51_000, date);
        contreVisite.Vehicule = precedent.Vehicule;
        return contreVisite;
    }

    [Fact]
    public void Une_contre_visite_est_refusee_apres_un_controle_favorable()
    {
        var controle = _c.ControleCloture(Energie.Essence, CatalogueDeTest.D(_c.EssuieGlace, "3.4.1.b.1"));

        Assert.Throws<RegleMetierException>(() => ContreVisite(controle));
    }

    [Fact]
    public void Une_contre_visite_est_refusee_si_le_controle_n_est_pas_cloture()
    {
        Assert.Throws<RegleMetierException>(() => ContreVisite(_c.NouveauControle()));
    }

    [Fact]
    public void Une_contre_visite_est_refusee_au_dela_de_deux_mois()
    {
        var controle = _c.ControleCloture(Energie.Essence, CatalogueDeTest.D(_c.Plaquettes, "1.1.13.a.2"));

        var erreur = Assert.Throws<RegleMetierException>(() => ContreVisite(controle, joursApresLePeriodique: 62));
        Assert.Contains("nouveau contrôle technique périodique", erreur.Message);
    }

    [Fact]
    public void Une_contre_visite_reverifie_les_points_selon_l_annexe_I()
    {
        var controle = _c.ControleCloture(Energie.Essence,
            CatalogueDeTest.D(_c.Plaquettes, "1.1.13.a.2"), CatalogueDeTest.D(_c.EssuieGlace, "3.4.1.b.1"));

        var codes = ContreVisite(controle).PointsASaisir(_c.Points).Select(p => p.Code).Order().ToList();

        Assert.Equal(["0.1.1", "1.1.13", "1.2.2", "7.11.1"], codes);
    }

    [Fact]
    public void Un_point_hors_contre_visite_ne_peut_pas_etre_saisi()
    {
        var contreVisite = ContreVisite(_c.ControleCloture(Energie.Essence, CatalogueDeTest.D(_c.Plaquettes, "1.1.13.a.2")));

        Assert.Throws<RegleMetierException>(() => contreVisite.Saisir(null,
            [new SaisiePoint(_c.EssuieGlace.Id, EtatPoint.Conforme, [], null)], _c.Points));
    }

    [Fact]
    public void Une_contre_visite_favorable_est_valide_deux_ans_depuis_le_controle_periodique()
    {
        var contreVisite = ContreVisite(_c.ControleCloture(Energie.Essence, CatalogueDeTest.D(_c.Plaquettes, "1.1.13.a.2")));
        contreVisite.Saisir(null, _c.ToutConforme(contreVisite), _c.Points);

        contreVisite.Cloturer(_c.Points, CatalogueDeTest.DateControle.AddDays(20));

        Assert.Equal(ResultatControle.Favorable, contreVisite.Resultat);
        Assert.Equal(new DateOnly(2028, 9, 1), contreVisite.DateFinValidite);
    }

    [Fact]
    public void Une_nouvelle_contre_visite_reste_possible_dans_le_delai_du_controle_periodique()
    {
        var premiere = ContreVisite(_c.ControleCloture(Energie.Essence, CatalogueDeTest.D(_c.Plaquettes, "1.1.13.a.2")));
        premiere.Saisir(null, _c.ToutConforme(premiere, CatalogueDeTest.D(_c.FreinService, "1.2.2.a.2")), _c.Points);
        premiere.Cloturer(_c.Points, CatalogueDeTest.DateControle.AddDays(20));

        var seconde = ContreVisite(premiere, joursApresLePeriodique: 50);

        Assert.Equal(ResultatControle.DefavorableMajeur, premiere.Resultat);
        Assert.Equal(new DateOnly(2026, 11, 1), premiere.DateLimiteContreVisite);
        Assert.Equal(CatalogueDeTest.DateControle, seconde.DateControlePeriodique);
        Assert.Throws<RegleMetierException>(() => ContreVisite(premiere, joursApresLePeriodique: 70));
    }
}
