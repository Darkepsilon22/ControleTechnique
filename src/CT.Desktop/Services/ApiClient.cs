using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CT.Shared.Dtos;
using CT.Shared.Enums;

namespace CT.Desktop.Services;

public class ApiException(string message, HttpStatusCode? statut = null) : Exception(message)
{
    public HttpStatusCode? Statut { get; } = statut;
}

public class ApiClient(HttpClient http, Session session)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<ConnexionReponse> ConnecterAsync(string email, string motDePasse)
    {
        var reponse = await EnvoyerAsync(HttpMethod.Post, "api/auth/login", new ConnexionRequete(email, motDePasse), authentifie: false);
        return (await LireAsync<ConnexionReponse>(reponse))!;
    }

    public Task<List<UtilisateurDto>> ListerUtilisateursAsync() => GetAsync<List<UtilisateurDto>>("api/utilisateurs");
    public Task<UtilisateurDto> CreerUtilisateurAsync(CreerUtilisateurRequete r) => PostAsync<UtilisateurDto>("api/utilisateurs", r);
    public Task<UtilisateurDto> ModifierUtilisateurAsync(Guid id, ModifierUtilisateurRequete r) => PutAsync<UtilisateurDto>($"api/utilisateurs/{id}", r);

    public Task<PageResultat<ProprietaireDto>> RechercherProprietairesAsync(string? recherche, int page = 1) =>
        GetAsync<PageResultat<ProprietaireDto>>($"api/proprietaires?search={Uri.EscapeDataString(recherche ?? "")}&page={page}");
    public Task<ProprietaireDto> CreerProprietaireAsync(ProprietaireRequete r) => PostAsync<ProprietaireDto>("api/proprietaires", r);
    public Task<ProprietaireDto> ModifierProprietaireAsync(Guid id, ProprietaireRequete r) => PutAsync<ProprietaireDto>($"api/proprietaires/{id}", r);

    public Task<PageResultat<VehiculeDto>> RechercherVehiculesAsync(string? recherche, int page = 1) =>
        GetAsync<PageResultat<VehiculeDto>>($"api/vehicules?search={Uri.EscapeDataString(recherche ?? "")}&page={page}");
    public Task<VehiculeDto> CreerVehiculeAsync(VehiculeRequete r) => PostAsync<VehiculeDto>("api/vehicules", r);
    public Task<VehiculeDto> ModifierVehiculeAsync(Guid id, VehiculeRequete r) => PutAsync<VehiculeDto>($"api/vehicules/{id}", r);
    public Task<List<ControleResumeDto>> HistoriqueVehiculeAsync(Guid id) => GetAsync<List<ControleResumeDto>>($"api/vehicules/{id}/controles");

    public Task<List<PointControleDto>> ListerPointsAsync() => GetAsync<List<PointControleDto>>("api/points-controle");
    public Task<PointControleDto> EnregistrerPointAsync(PointControleRequete r) => PostAsync<PointControleDto>("api/points-controle", r);
    public Task<PointControleDto> EnregistrerDefaillanceAsync(Guid pointId, DefaillanceRequete r) =>
        PostAsync<PointControleDto>($"api/points-controle/{pointId}/defaillances", r);

    public Task<PageResultat<ControleResumeDto>> ListerControlesAsync(StatutControle? statut, string? recherche, bool mesControles, int page = 1) =>
        GetAsync<PageResultat<ControleResumeDto>>(
            $"api/controles?search={Uri.EscapeDataString(recherche ?? "")}&mesControles={mesControles}&page={page}"
            + (statut is null ? "" : $"&statut={statut}"));
    public Task<ControleDto> ObtenirControleAsync(Guid id) => GetAsync<ControleDto>($"api/controles/{id}");
    public Task<ControleDto> OuvrirControleAsync(OuvrirControleRequete r) => PostAsync<ControleDto>("api/controles", r);
    public Task<ControleDto> OuvrirContreVisiteAsync(Guid controleInitialId, OuvrirContreVisiteRequete r) =>
        PostAsync<ControleDto>($"api/controles/{controleInitialId}/contre-visite", r);
    public Task<ControleDto> SaisirControleAsync(Guid id, SaisirControleRequete r) => PutAsync<ControleDto>($"api/controles/{id}", r);
    public Task<ControleDto> CloturerControleAsync(Guid id) => PostAsync<ControleDto>($"api/controles/{id}/cloturer", null);

    public async Task<(byte[] Contenu, string NomFichier)> TelechargerPvAsync(Guid id)
    {
        var reponse = await EnvoyerAsync(HttpMethod.Get, $"api/controles/{id}/pv");
        var nom = reponse.Content.Headers.ContentDisposition?.FileNameStar
            ?? reponse.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"PV_{id}.pdf";
        return (await reponse.Content.ReadAsByteArrayAsync(), nom);
    }

    public Task<StatistiquesDto> StatistiquesAsync(DateOnly du, DateOnly au) =>
        GetAsync<StatistiquesDto>($"api/statistiques/resume?du={du:yyyy-MM-dd}&au={au:yyyy-MM-dd}");

    private async Task<T> GetAsync<T>(string url) =>
        (await LireAsync<T>(await EnvoyerAsync(HttpMethod.Get, url)))!;

    private async Task<T> PostAsync<T>(string url, object? corps) =>
        (await LireAsync<T>(await EnvoyerAsync(HttpMethod.Post, url, corps)))!;

    private async Task<T> PutAsync<T>(string url, object corps) =>
        (await LireAsync<T>(await EnvoyerAsync(HttpMethod.Put, url, corps)))!;

    private static Task<T?> LireAsync<T>(HttpResponseMessage reponse) => reponse.Content.ReadFromJsonAsync<T>(Json);

    private async Task<HttpResponseMessage> EnvoyerAsync(HttpMethod methode, string url, object? corps = null, bool authentifie = true)
    {
        var requete = new HttpRequestMessage(methode, url);
        if (corps is not null)
            requete.Content = JsonContent.Create(corps, corps.GetType(), options: Json);
        if (authentifie && session.Jeton is not null)
            requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Jeton);

        HttpResponseMessage reponse;
        try
        {
            reponse = await http.SendAsync(requete);
        }
        catch (HttpRequestException)
        {
            throw new ApiException($"Impossible de joindre l'API ({http.BaseAddress}). Vérifiez qu'elle est démarrée avec dotnet run.");
        }

        if (reponse.IsSuccessStatusCode)
            return reponse;

        throw new ApiException(await MessageErreurAsync(reponse), reponse.StatusCode);
    }

    private static async Task<string> MessageErreurAsync(HttpResponseMessage reponse)
    {
        switch (reponse.StatusCode)
        {
            case HttpStatusCode.Unauthorized when reponse.RequestMessage?.Headers.Authorization is not null:
                return "Session expirée : reconnectez-vous.";
            case HttpStatusCode.Forbidden:
                return "Vous n'avez pas les droits pour cette action.";
        }

        try
        {
            var probleme = await reponse.Content.ReadFromJsonAsync<JsonElement>(Json);
            if (probleme.TryGetProperty("errors", out var erreurs))
                return string.Join(Environment.NewLine, erreurs.EnumerateObject()
                    .SelectMany(e => e.Value.EnumerateArray().Select(m => m.GetString())));
            if (probleme.TryGetProperty("detail", out var detail) && detail.GetString() is { Length: > 0 } texte)
                return texte;
            if (probleme.TryGetProperty("title", out var titre))
                return titre.GetString() ?? reponse.ReasonPhrase ?? "Erreur";
        }
        catch (JsonException)
        {
        }

        return $"Erreur {(int)reponse.StatusCode} : {reponse.ReasonPhrase}";
    }
}
