using System.Net;
using System.Net.Http.Json;
using CT.Infrastructure.Data;
using CT.Shared.Dtos;
using CT.Shared.Enums;

namespace CT.Tests.Api;

public class ApiTests(CtApiFactory factory) : IClassFixture<CtApiFactory>
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = CtApiFactory.Json;

    [Fact]
    public async Task Une_connexion_valide_renvoie_un_jeton()
    {
        var client = factory.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/auth/login", new ConnexionRequete(DbSeeder.EmailAdmin, DbSeeder.MotDePasseDemo), Json);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var connexion = await reponse.Content.ReadFromJsonAsync<ConnexionReponse>(Json);
        Assert.False(string.IsNullOrEmpty(connexion!.Jeton));
        Assert.Equal(Role.Administrateur, connexion.Utilisateur.Role);
    }

    [Fact]
    public async Task Une_connexion_avec_un_mauvais_mot_de_passe_renvoie_401()
    {
        var client = factory.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/auth/login", new ConnexionRequete(DbSeeder.EmailAdmin, "mauvais"), Json);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task Une_requete_sans_jeton_renvoie_401()
    {
        var reponse = await factory.CreateClient().GetAsync("/api/vehicules");

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task Un_inspecteur_ne_peut_pas_creer_de_vehicule()
    {
        var client = await factory.ClientConnecteAsync(DbSeeder.EmailInspecteur);
        var proprietaire = await PremierProprietaireAsync();

        var reponse = await client.PostAsJsonAsync("/api/vehicules", Vehicule("IN-001-SP", "VF1INSPECT0000001", proprietaire), Json);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task La_reception_ne_peut_pas_consulter_les_statistiques()
    {
        var client = await factory.ClientConnecteAsync(DbSeeder.EmailReception);

        var reponse = await client.GetAsync("/api/statistiques/resume");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task Une_immatriculation_en_double_renvoie_409()
    {
        var client = await factory.ClientConnecteAsync(DbSeeder.EmailReception);
        var proprietaire = await PremierProprietaireAsync();

        var premier = await client.PostAsJsonAsync("/api/vehicules", Vehicule("DB-123-LE", "VF1DOUBLON0000001", proprietaire), Json);
        var second = await client.PostAsJsonAsync("/api/vehicules", Vehicule(" db-123-le ", "VF1DOUBLON0000002", proprietaire), Json);

        Assert.Equal(HttpStatusCode.Created, premier.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Des_donnees_invalides_renvoient_400()
    {
        var client = await factory.ClientConnecteAsync(DbSeeder.EmailReception);
        var proprietaire = await PremierProprietaireAsync();

        var reponse = await client.PostAsJsonAsync("/api/vehicules", Vehicule("IV-001-AL", "TROP-COURT", proprietaire), Json);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task La_recherche_de_vehicules_est_paginee()
    {
        var client = await factory.ClientConnecteAsync(DbSeeder.EmailInspecteur);

        var page = await client.GetFromJsonAsync<PageResultat<VehiculeDto>>("/api/vehicules?page=2", Json);

        Assert.Equal(Pagination.TaillePageParDefaut, page!.Elements.Count);
        Assert.Equal(2, page.Page);
        Assert.True(page.Total >= 200);
    }

    [Fact]
    public async Task Un_controle_complet_va_de_l_ouverture_au_pv()
    {
        var reception = await factory.ClientConnecteAsync(DbSeeder.EmailReception);
        var inspecteur = await factory.ClientConnecteAsync(DbSeeder.EmailInspecteur);
        var proprietaire = await PremierProprietaireAsync();
        var vehicule = await (await reception.PostAsJsonAsync("/api/vehicules", Vehicule("PA-100-RC", "VF1PARCOURS000001", proprietaire), Json))
            .Content.ReadFromJsonAsync<VehiculeDto>(Json);

        var ouverture = await inspecteur.PostAsJsonAsync("/api/controles", new OuvrirControleRequete(vehicule!.Id, 120_000), Json);
        Assert.Equal(HttpStatusCode.Created, ouverture.StatusCode);
        var controle = await ouverture.Content.ReadFromJsonAsync<ControleDto>(Json);

        var pvAvantCloture = await reception.GetAsync($"/api/controles/{controle!.Id}/pv");
        Assert.Equal(HttpStatusCode.Conflict, pvAvantCloture.StatusCode);

        var catalogue = await CatalogueAsync(inspecteur);
        var saisie = await inspecteur.PutAsJsonAsync($"/api/controles/{controle.Id}",
            new SaisirControleRequete(null, Saisies(controle, catalogue, "3.4.1.b.1")), Json);
        Assert.Equal(HttpStatusCode.OK, saisie.StatusCode);

        var cloture = await inspecteur.PostAsync($"/api/controles/{controle.Id}/cloturer", null);
        Assert.Equal(HttpStatusCode.OK, cloture.StatusCode);
        var clotureDto = await cloture.Content.ReadFromJsonAsync<ControleDto>(Json);
        Assert.Equal(ResultatControle.Favorable, clotureDto!.Resultat);
        Assert.Equal(["3.4.1.b.1"], clotureDto.DefaillancesConstatees.Select(d => d.Code));
        Assert.Equal(DateOnly.FromDateTime(controle.DateControle).AddYears(2), clotureDto.DateFinValidite);

        var modification = await inspecteur.PutAsJsonAsync($"/api/controles/{controle.Id}", new SaisirControleRequete(1, []), Json);
        Assert.Equal(HttpStatusCode.Conflict, modification.StatusCode);

        var pv = await reception.GetAsync($"/api/controles/{controle.Id}/pv");
        Assert.Equal(HttpStatusCode.OK, pv.StatusCode);
        Assert.Equal("application/pdf", pv.Content.Headers.ContentType!.MediaType);

        var vehiculeApres = await reception.GetFromJsonAsync<VehiculeDto>($"/api/vehicules/{vehicule.Id}", Json);
        Assert.Equal(StatutEcheance.AJour, vehiculeApres!.Echeance);
        Assert.Equal(clotureDto.DateFinValidite, vehiculeApres.DateLimiteControle);
    }

    [Fact]
    public async Task Une_defaillance_sans_lien_avec_le_point_est_refusee()
    {
        var reception = await factory.ClientConnecteAsync(DbSeeder.EmailReception);
        var inspecteur = await factory.ClientConnecteAsync(DbSeeder.EmailInspecteur);
        var vehicule = await (await reception.PostAsJsonAsync("/api/vehicules",
                Vehicule("DF-300-PT", "VF1DEFAILLANCE001", await PremierProprietaireAsync()), Json))
            .Content.ReadFromJsonAsync<VehiculeDto>(Json);
        var controle = await (await inspecteur.PostAsJsonAsync("/api/controles", new OuvrirControleRequete(vehicule!.Id, 50_000), Json))
            .Content.ReadFromJsonAsync<ControleDto>(Json);
        var catalogue = await CatalogueAsync(inspecteur);
        var freins = catalogue.Single(p => p.Code == "1.1.13");
        var feuxStop = catalogue.Single(p => p.Code == "4.3.1");

        var reponse = await inspecteur.PutAsJsonAsync($"/api/controles/{controle!.Id}", new SaisirControleRequete(null,
            [new SaisiePointRequete(freins.Id, EtatPoint.NonConforme, [feuxStop.Defaillances[0].Id], null)]), Json);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Une_contre_visite_reverifie_les_points_selon_l_annexe_I()
    {
        var reception = await factory.ClientConnecteAsync(DbSeeder.EmailReception);
        var inspecteur = await factory.ClientConnecteAsync(DbSeeder.EmailInspecteur);
        var proprietaire = await PremierProprietaireAsync();
        var vehicule = await (await reception.PostAsJsonAsync("/api/vehicules", Vehicule("CV-200-RE", "VF1CONTREVISITE01", proprietaire), Json))
            .Content.ReadFromJsonAsync<VehiculeDto>(Json);
        var catalogue = await CatalogueAsync(inspecteur);

        var initial = await (await inspecteur.PostAsJsonAsync("/api/controles", new OuvrirControleRequete(vehicule!.Id, 90_000), Json))
            .Content.ReadFromJsonAsync<ControleDto>(Json);
        await inspecteur.PutAsJsonAsync($"/api/controles/{initial!.Id}",
            new SaisirControleRequete(null, Saisies(initial, catalogue, "4.3.1.a.3", "3.4.1.b.1")), Json);
        var initialCloture = await (await inspecteur.PostAsync($"/api/controles/{initial.Id}/cloturer", null))
            .Content.ReadFromJsonAsync<ControleDto>(Json);
        Assert.Equal(ResultatControle.DefavorableCritique, initialCloture!.Resultat);
        Assert.Equal(DateOnly.FromDateTime(initial.DateControle), initialCloture.DateFinValidite);

        var ouverture = await inspecteur.PostAsJsonAsync($"/api/controles/{initial.Id}/contre-visite", new OuvrirContreVisiteRequete(90_500), Json);
        Assert.Equal(HttpStatusCode.Created, ouverture.StatusCode);
        var contreVisite = await ouverture.Content.ReadFromJsonAsync<ControleDto>(Json);
        var codes = catalogue.Where(p => contreVisite!.PointsASaisir.Contains(p.Id)).Select(p => p.Code).ToList();
        Assert.Contains("4.3.1", codes);
        Assert.Contains("0.1.1", codes);
        Assert.Contains("7.11.1", codes);
        Assert.DoesNotContain("3.4.1", codes);

        var hors = await inspecteur.PutAsJsonAsync($"/api/controles/{contreVisite!.Id}", new SaisirControleRequete(null,
            [new SaisiePointRequete(catalogue.Single(p => p.Code == "3.4.1").Id, EtatPoint.Conforme, [], null)]), Json);
        Assert.Equal(HttpStatusCode.BadRequest, hors.StatusCode);

        await inspecteur.PutAsJsonAsync($"/api/controles/{contreVisite.Id}",
            new SaisirControleRequete(null, Saisies(contreVisite, catalogue)), Json);
        var cloture = await (await inspecteur.PostAsync($"/api/controles/{contreVisite.Id}/cloturer", null))
            .Content.ReadFromJsonAsync<ControleDto>(Json);
        Assert.Equal(ResultatControle.Favorable, cloture!.Resultat);
        Assert.Equal(DateOnly.FromDateTime(initial.DateControle).AddYears(2), cloture.DateFinValidite);

        var seconde = await inspecteur.PostAsJsonAsync($"/api/controles/{initial.Id}/contre-visite", new OuvrirContreVisiteRequete(91_000), Json);
        Assert.Equal(HttpStatusCode.Conflict, seconde.StatusCode);

        var pv = await reception.GetAsync($"/api/controles/{contreVisite.Id}/pv");
        Assert.Equal(HttpStatusCode.OK, pv.StatusCode);
    }

    [Fact]
    public async Task Un_inspecteur_ne_peut_pas_modifier_le_controle_d_un_autre()
    {
        var admin = await factory.ClientConnecteAsync(DbSeeder.EmailAdmin);
        var creation = await admin.PostAsJsonAsync("/api/utilisateurs",
            new CreerUtilisateurRequete("Second Inspecteur", "inspecteur2@ct.local", DbSeeder.MotDePasseDemo, Role.Inspecteur), Json);
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);

        var inspecteur = await factory.ClientConnecteAsync(DbSeeder.EmailInspecteur);
        var autre = await factory.ClientConnecteAsync("inspecteur2@ct.local");
        var vehicules = await inspecteur.GetFromJsonAsync<PageResultat<VehiculeDto>>("/api/vehicules?page=3", Json);
        var controle = await (await inspecteur.PostAsJsonAsync("/api/controles", new OuvrirControleRequete(vehicules!.Elements[0].Id, 10_000), Json))
            .Content.ReadFromJsonAsync<ControleDto>(Json);

        var reponse = await autre.PostAsync($"/api/controles/{controle!.Id}/cloturer", null);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task L_administrateur_obtient_les_statistiques()
    {
        var admin = await factory.ClientConnecteAsync(DbSeeder.EmailAdmin);

        var stats = await admin.GetFromJsonAsync<StatistiquesDto>("/api/statistiques/resume", Json);

        Assert.True(stats!.TotalControles > 0);
        Assert.Equal(stats.TotalControles, stats.Favorables + stats.DefavorablesMajeurs + stats.DefavorablesCritiques);
        Assert.NotEmpty(stats.ParMois);
    }

    private async Task<Guid> PremierProprietaireAsync()
    {
        var reception = await factory.ClientConnecteAsync(DbSeeder.EmailReception);
        var page = await reception.GetFromJsonAsync<PageResultat<ProprietaireDto>>("/api/proprietaires", Json);
        return page!.Elements[0].Id;
    }

    private static async Task<List<PointControleDto>> CatalogueAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<PointControleDto>>("/api/points-controle", Json))!;

    private static List<SaisiePointRequete> Saisies(ControleDto controle, List<PointControleDto> catalogue, params string[] defaillances) =>
        catalogue.Where(p => controle.PointsASaisir.Contains(p.Id)).Select(p =>
        {
            var constatees = p.Defaillances.Where(d => defaillances.Contains(d.Code)).Select(d => d.Id).ToList();
            return new SaisiePointRequete(p.Id, constatees.Count > 0 ? EtatPoint.NonConforme : EtatPoint.Conforme, constatees, null);
        }).ToList();

    private static VehiculeRequete Vehicule(string immatriculation, string chassis, Guid proprietaireId) =>
        new(immatriculation, chassis, "Peugeot", "208", new DateOnly(2019, 6, 1), TypeVehicule.VoitureParticuliere, Energie.Essence, proprietaireId);
}
