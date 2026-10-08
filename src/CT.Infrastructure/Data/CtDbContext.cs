using CT.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CT.Infrastructure.Data;

public class CtDbContext(DbContextOptions<CtDbContext> options) : DbContext(options)
{
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Proprietaire> Proprietaires => Set<Proprietaire>();
    public DbSet<Vehicule> Vehicules => Set<Vehicule>();
    public DbSet<CategoriePoint> CategoriesPoints => Set<CategoriePoint>();
    public DbSet<PointControle> PointsControle => Set<PointControle>();
    public DbSet<Controle> Controles => Set<Controle>();
    public DbSet<ResultatPoint> ResultatsPoints => Set<ResultatPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CtDbContext).Assembly);

        // GUID créés par le code : sinon EF prend une ligne ajoutée à une collection pour une ligne existante.
        foreach (var entite in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var cle in entite.FindPrimaryKey()?.Properties ?? [])
                cle.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
    }
}
