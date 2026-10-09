using CT.Domain.Entities;
using CT.Domain.Enums;
using CT.Domain.Exceptions;

namespace CT.Tests.Domain;

public class ContreVisiteTests
{
    private static readonly DateTime DateInitiale = new(2026, 9, 1, 10, 0, 0);

    private readonly PointControle _freins = new() { Libelle = "Freins", Gravite = Gravite.Critique };
    private readonly PointControle _feux = new() { Libelle = "Feux", Gravite = Gravite.Majeur };
    private readonly PointControle _essuieGlaces = new() { Libelle = "Essuie-glaces", Gravite = Gravite.Mineur };

    private IEnumerable<PointControle> Points => [_freins, _feux, _essuieGlaces];
    private IEnumerable<Guid> PointsActifs => Points.Select(p => p.Id);

    private Controle ControleCloture(EtatPoint etatFreins)
    {
        var controle = Controle.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), DateInitiale, 50_000, DateInitiale);
        controle.Saisir(null,
        [
            new SaisiePoint(_freins.Id, etatFreins, null),
            new SaisiePoint(_feux.Id, EtatPoint.Conforme, null),
            new SaisiePoint(_essuieGlaces.Id, EtatPoint.NonConforme, null)
        ]);
        RattacherPoints(controle);
        controle.Cloturer(PointsActifs, 12, DateInitiale);
        return controle;
    }

    private void RattacherPoints(Controle controle)
    {
        foreach (var resultat in controle.Resultats)
            resultat.PointControle = Points.Single(p => p.Id == resultat.PointControleId);
    }

    private static Controle ContreVisite(Controle initial, int joursApres = 20, int kilometrage = 51_000) =>
        Controle.OuvrirContreVisite(initial, Guid.NewGuid(), DateInitiale.AddDays(joursApres), kilometrage,
            DateInitiale.AddDays(joursApres), delaiMois: 2);

    [Fact]
    public void Une_contre_visite_est_refusee_apres_un_controle_favorable()
    {
        var initial = ControleCloture(EtatPoint.Conforme);

        Assert.Throws<RegleMetierException>(() => ContreVisite(initial));
    }

    [Fact]
    public void Une_contre_visite_est_refusee_si_le_controle_initial_n_est_pas_cloture()
    {
        var initial = Controle.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), DateInitiale, 50_000, DateInitiale);

        Assert.Throws<RegleMetierException>(() => ContreVisite(initial));
    }

    [Fact]
    public void Une_contre_visite_est_refusee_apres_le_delai()
    {
        var initial = ControleCloture(EtatPoint.NonConforme);

        var erreur = Assert.Throws<RegleMetierException>(() => ContreVisite(initial, joursApres: 70));
        Assert.Contains("délai", erreur.Message);
    }

    [Fact]
    public void Un_kilometrage_inferieur_au_controle_initial_est_refuse()
    {
        var initial = ControleCloture(EtatPoint.NonConforme);

        Assert.Throws<RegleMetierException>(() => ContreVisite(initial, kilometrage: 49_000));
    }

    [Fact]
    public void Une_contre_visite_ne_porte_que_sur_les_points_non_conformes_du_controle_initial()
    {
        var initial = ControleCloture(EtatPoint.NonConforme);
        var contreVisite = ContreVisite(initial);

        Assert.Equal(new[] { _freins.Id, _essuieGlaces.Id }.Order(), contreVisite.PointsAVerifier(PointsActifs).Order());
        Assert.Throws<RegleMetierException>(() =>
            contreVisite.Saisir(null, [new SaisiePoint(_feux.Id, EtatPoint.Conforme, null)]));
    }

    [Fact]
    public void Une_contre_visite_reussie_est_favorable_et_valide_depuis_la_date_du_controle_initial()
    {
        var initial = ControleCloture(EtatPoint.NonConforme);
        var contreVisite = ContreVisite(initial);
        contreVisite.Saisir(null,
        [
            new SaisiePoint(_freins.Id, EtatPoint.Conforme, "Disques remplacés"),
            new SaisiePoint(_essuieGlaces.Id, EtatPoint.Conforme, null)
        ]);
        RattacherPoints(contreVisite);

        contreVisite.Cloturer(PointsActifs, 12, DateInitiale.AddDays(20));

        Assert.Equal(ResultatControle.Favorable, contreVisite.Resultat);
        Assert.Equal(new DateOnly(2027, 9, 1), contreVisite.DateFinValidite);
    }

    [Fact]
    public void La_cloture_d_une_contre_visite_exige_tous_les_points_a_reverifier()
    {
        var initial = ControleCloture(EtatPoint.NonConforme);
        var contreVisite = ContreVisite(initial);
        contreVisite.Saisir(null, [new SaisiePoint(_freins.Id, EtatPoint.Conforme, null)]);
        RattacherPoints(contreVisite);

        var erreur = Assert.Throws<RegleMetierException>(() => contreVisite.Cloturer(PointsActifs, 12, DateInitiale));
        Assert.Contains("1 point(s)", erreur.Message);
    }

    [Fact]
    public void Une_contre_visite_defavorable_n_accepte_pas_de_nouvelle_contre_visite()
    {
        var initial = ControleCloture(EtatPoint.NonConforme);
        var contreVisite = ContreVisite(initial);
        contreVisite.Saisir(null,
        [
            new SaisiePoint(_freins.Id, EtatPoint.NonConforme, null),
            new SaisiePoint(_essuieGlaces.Id, EtatPoint.Conforme, null)
        ]);
        RattacherPoints(contreVisite);
        contreVisite.Cloturer(PointsActifs, 12, DateInitiale.AddDays(20));

        Assert.Equal(ResultatControle.Defavorable, contreVisite.Resultat);
        Assert.Null(contreVisite.DateFinValidite);
        Assert.Throws<RegleMetierException>(() => ContreVisite(contreVisite, joursApres: 30));
    }
}
