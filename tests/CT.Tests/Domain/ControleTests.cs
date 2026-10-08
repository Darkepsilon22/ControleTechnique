using CT.Domain.Entities;
using CT.Domain.Enums;
using CT.Domain.Exceptions;

namespace CT.Tests.Domain;

public class ControleTests
{
    private static readonly DateTime Maintenant = new(2026, 10, 8, 10, 0, 0);

    private readonly PointControle _freins = new() { Libelle = "Freins", Gravite = Gravite.Critique };
    private readonly PointControle _feux = new() { Libelle = "Feux", Gravite = Gravite.Majeur };
    private readonly PointControle _essuieGlaces = new() { Libelle = "Essuie-glaces", Gravite = Gravite.Mineur };

    private IEnumerable<PointControle> Points => [_freins, _feux, _essuieGlaces];
    private IEnumerable<Guid> PointsActifs => Points.Select(p => p.Id);

    private Controle NouveauControle() => Controle.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), Maintenant, 50_000, Maintenant);

    private void SaisirTout(Controle controle, EtatPoint etatFreins = EtatPoint.Conforme, EtatPoint etatEssuieGlaces = EtatPoint.Conforme)
    {
        controle.Saisir(null,
        [
            new SaisiePoint(_freins.Id, etatFreins, null),
            new SaisiePoint(_feux.Id, EtatPoint.Conforme, null),
            new SaisiePoint(_essuieGlaces.Id, etatEssuieGlaces, "Balais usés")
        ]);
        foreach (var resultat in controle.Resultats)
            resultat.PointControle = Points.Single(p => p.Id == resultat.PointControleId);
    }

    [Fact]
    public void Un_controle_ouvert_est_en_brouillon()
    {
        var controle = NouveauControle();

        Assert.Equal(StatutControle.Brouillon, controle.Statut);
        Assert.Null(controle.Resultat);
    }

    [Fact]
    public void Un_kilometrage_negatif_est_refuse()
    {
        Assert.Throws<RegleMetierException>(() => Controle.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), Maintenant, -1, Maintenant));
    }

    [Fact]
    public void Une_date_de_controle_dans_le_futur_est_refusee()
    {
        Assert.Throws<RegleMetierException>(() =>
            Controle.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), Maintenant.AddDays(3), 1000, Maintenant));
    }

    [Fact]
    public void Un_point_ne_peut_pas_etre_saisi_deux_fois_dans_la_meme_requete()
    {
        var controle = NouveauControle();

        Assert.Throws<RegleMetierException>(() => controle.Saisir(null,
        [
            new SaisiePoint(_freins.Id, EtatPoint.Conforme, null),
            new SaisiePoint(_freins.Id, EtatPoint.NonConforme, null)
        ]));
    }

    [Fact]
    public void Une_nouvelle_saisie_remplace_le_resultat_existant_sans_doublon()
    {
        var controle = NouveauControle();
        controle.Saisir(null, [new SaisiePoint(_freins.Id, EtatPoint.Conforme, null)]);

        controle.Saisir(52_000, [new SaisiePoint(_freins.Id, EtatPoint.NonConforme, "Disque fissuré")]);

        var resultat = Assert.Single(controle.Resultats);
        Assert.Equal(EtatPoint.NonConforme, resultat.Etat);
        Assert.Equal("Disque fissuré", resultat.Commentaire);
        Assert.Equal(52_000, controle.Kilometrage);
    }

    [Fact]
    public void Un_commentaire_vide_est_enregistre_comme_absent()
    {
        var controle = NouveauControle();

        controle.Saisir(null, [new SaisiePoint(_freins.Id, EtatPoint.Conforme, "   ")]);

        Assert.Null(controle.Resultats.Single().Commentaire);
    }

    [Fact]
    public void La_cloture_est_refusee_si_des_points_actifs_ne_sont_pas_saisis()
    {
        var controle = NouveauControle();
        controle.Saisir(null, [new SaisiePoint(_freins.Id, EtatPoint.Conforme, null)]);
        controle.Resultats[0].PointControle = _freins;

        var erreur = Assert.Throws<RegleMetierException>(() => controle.Cloturer(PointsActifs, 12, Maintenant));
        Assert.Contains("2 point(s)", erreur.Message);
    }

    [Fact]
    public void La_cloture_calcule_un_resultat_favorable_avec_observations_et_validite()
    {
        var controle = NouveauControle();
        SaisirTout(controle, etatEssuieGlaces: EtatPoint.NonConforme);

        controle.Cloturer(PointsActifs, 12, Maintenant.AddHours(1));

        Assert.Equal(StatutControle.Cloture, controle.Statut);
        Assert.Equal(ResultatControle.Favorable, controle.Resultat);
        Assert.Equal("Essuie-glaces", controle.Observations);
        Assert.Equal(new DateOnly(2027, 10, 8), controle.DateFinValidite);
        Assert.Equal(Maintenant.AddHours(1), controle.ClotureLe);
    }

    [Fact]
    public void La_cloture_d_un_controle_defavorable_ne_donne_aucune_date_de_validite()
    {
        var controle = NouveauControle();
        SaisirTout(controle, etatFreins: EtatPoint.NonConforme);

        controle.Cloturer(PointsActifs, 12, Maintenant);

        Assert.Equal(ResultatControle.Defavorable, controle.Resultat);
        Assert.Null(controle.DateFinValidite);
    }

    [Fact]
    public void Un_controle_cloture_refuse_toute_nouvelle_saisie()
    {
        var controle = NouveauControle();
        SaisirTout(controle);
        controle.Cloturer(PointsActifs, 12, Maintenant);

        Assert.Throws<ControleClotureException>(() =>
            controle.Saisir(60_000, [new SaisiePoint(_freins.Id, EtatPoint.NonConforme, null)]));
        Assert.All(controle.Resultats, r => Assert.Equal(EtatPoint.Conforme, r.Etat));
    }

    [Fact]
    public void Un_controle_ne_peut_pas_etre_cloture_deux_fois()
    {
        var controle = NouveauControle();
        SaisirTout(controle);
        controle.Cloturer(PointsActifs, 12, Maintenant);

        Assert.Throws<ControleClotureException>(() => controle.Cloturer(PointsActifs, 12, Maintenant));
    }

    [Fact]
    public void Un_point_desactive_deja_saisi_reste_pris_en_compte_dans_le_resultat()
    {
        var controle = NouveauControle();
        SaisirTout(controle, etatFreins: EtatPoint.NonConforme);

        controle.Cloturer(PointsActifs.Where(id => id != _freins.Id), 12, Maintenant);

        Assert.Equal(ResultatControle.Defavorable, controle.Resultat);
    }
}
