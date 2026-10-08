using CT.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CT.Infrastructure.Data.Configurations;

public class UtilisateurConfiguration : IEntityTypeConfiguration<Utilisateur>
{
    public void Configure(EntityTypeBuilder<Utilisateur> builder)
    {
        builder.ToTable("Utilisateur");
        builder.Property(u => u.NomComplet).HasMaxLength(100);
        builder.Property(u => u.Email).HasMaxLength(150);
        builder.Property(u => u.MotDePasseHash).HasMaxLength(100);
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

public class ProprietaireConfiguration : IEntityTypeConfiguration<Proprietaire>
{
    public void Configure(EntityTypeBuilder<Proprietaire> builder)
    {
        builder.ToTable("Proprietaire");
        builder.Property(p => p.Nom).HasMaxLength(100);
        builder.Property(p => p.Telephone).HasMaxLength(30);
        builder.Property(p => p.Adresse).HasMaxLength(250);
        builder.HasIndex(p => p.Nom);
    }
}

public class VehiculeConfiguration : IEntityTypeConfiguration<Vehicule>
{
    public void Configure(EntityTypeBuilder<Vehicule> builder)
    {
        builder.ToTable("Vehicule");
        builder.Property(v => v.Immatriculation).HasMaxLength(20);
        builder.Property(v => v.NumeroChassis).HasMaxLength(17);
        builder.Property(v => v.Marque).HasMaxLength(50);
        builder.Property(v => v.Modele).HasMaxLength(50);
        builder.HasIndex(v => v.Immatriculation).IsUnique();
        builder.HasIndex(v => v.NumeroChassis).IsUnique();

        builder.HasOne(v => v.Proprietaire)
            .WithMany(p => p.Vehicules)
            .HasForeignKey(v => v.ProprietaireId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CategoriePointConfiguration : IEntityTypeConfiguration<CategoriePoint>
{
    public void Configure(EntityTypeBuilder<CategoriePoint> builder)
    {
        builder.ToTable("CategoriePoint");
        builder.Property(c => c.Libelle).HasMaxLength(60);
        builder.HasIndex(c => c.Libelle).IsUnique();
    }
}

public class PointControleConfiguration : IEntityTypeConfiguration<PointControle>
{
    public void Configure(EntityTypeBuilder<PointControle> builder)
    {
        builder.ToTable("PointControle");
        builder.Property(p => p.Libelle).HasMaxLength(150);

        builder.HasOne(p => p.Categorie)
            .WithMany(c => c.Points)
            .HasForeignKey(p => p.CategorieId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ControleConfiguration : IEntityTypeConfiguration<Controle>
{
    public void Configure(EntityTypeBuilder<Controle> builder)
    {
        builder.ToTable("Controle");
        builder.Property(c => c.Observations).HasMaxLength(2000);
        builder.Ignore(c => c.EstCloture);
        builder.HasIndex(c => c.DateControle);

        builder.HasOne(c => c.Vehicule)
            .WithMany(v => v.Controles)
            .HasForeignKey(c => c.VehiculeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Inspecteur)
            .WithMany(u => u.Controles)
            .HasForeignKey(c => c.InspecteurId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Resultats)
            .WithOne(r => r.Controle)
            .HasForeignKey(r => r.ControleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ResultatPointConfiguration : IEntityTypeConfiguration<ResultatPoint>
{
    public void Configure(EntityTypeBuilder<ResultatPoint> builder)
    {
        builder.ToTable("ResultatPoint");
        builder.Property(r => r.Commentaire).HasMaxLength(500);

        builder.HasIndex(r => new { r.ControleId, r.PointControleId }).IsUnique();

        builder.HasOne(r => r.PointControle)
            .WithMany()
            .HasForeignKey(r => r.PointControleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
