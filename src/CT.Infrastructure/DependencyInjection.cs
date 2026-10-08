using CT.Infrastructure.Data;
using CT.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CT.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CtDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton<IHachageMotDePasse, BCryptHachageMotDePasse>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }

    public static async Task InitialiserBaseAsync(this IServiceProvider services, int vehiculesDeCharge = 0, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CtDbContext>();

        if (db.Database.IsSqlServer())
            await db.Database.MigrateAsync(ct);
        else
            await db.Database.EnsureCreatedAsync(ct);

        await DbSeeder.SeedAsync(
            db,
            scope.ServiceProvider.GetRequiredService<IHachageMotDePasse>(),
            scope.ServiceProvider.GetRequiredService<TimeProvider>(),
            ct);

        if (vehiculesDeCharge > 0)
            await DbSeeder.AjouterVehiculesDeChargeAsync(db, vehiculesDeCharge, ct);
    }
}
