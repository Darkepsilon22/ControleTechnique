using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CT.Infrastructure.Data;
using CT.Shared.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CT.Tests.Api;

public class CtApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SqliteConnection _connexion = new("DataSource=:memory:");

    public CtApiFactory() => _connexion.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Jwt:Key", "cle-de-test-suffisamment-longue-pour-hmac-sha256");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<CtDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CtDbContext>>();
            services.AddDbContext<CtDbContext>(options => options.UseSqlite(_connexion));
        });
    }

    public async Task<HttpClient> ClientConnecteAsync(string email)
    {
        var client = CreateClient();
        var reponse = await client.PostAsJsonAsync("/api/auth/login", new ConnexionRequete(email, DbSeeder.MotDePasseDemo), Json);
        reponse.EnsureSuccessStatusCode();
        var connexion = await reponse.Content.ReadFromJsonAsync<ConnexionReponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connexion!.Jeton);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connexion.Dispose();
    }
}
