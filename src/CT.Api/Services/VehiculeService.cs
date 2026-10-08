using CT.Api.Mapping;
using CT.Domain.Entities;
using CT.Domain.Exceptions;
using CT.Infrastructure.Data;
using CT.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using D = CT.Domain.Enums;

namespace CT.Api.Services;

public interface IVehiculeService
{
    Task<PageResultat<VehiculeDto>> RechercherAsync(string? recherche, int page, int taillePage, CancellationToken ct);
    Task<VehiculeDto> ObtenirAsync(Guid id, CancellationToken ct);
    Task<VehiculeDto> CreerAsync(VehiculeRequete requete, CancellationToken ct);
    Task<VehiculeDto> ModifierAsync(Guid id, VehiculeRequete requete, CancellationToken ct);
    Task<IReadOnlyList<ControleResumeDto>> HistoriqueAsync(Guid id, CancellationToken ct);
}

public class VehiculeService(CtDbContext db) : IVehiculeService
{
    public Task<PageResultat<VehiculeDto>> RechercherAsync(string? recherche, int page, int taillePage, CancellationToken ct)
    {
        var requete = db.Vehicules.AsQueryable();
        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var terme = Vehicule.NormaliserImmatriculation(recherche);
            requete = requete.Where(v =>
                v.Immatriculation.Contains(terme) ||
                v.NumeroChassis.StartsWith(terme) ||
                v.Proprietaire!.Nom.Contains(recherche.Trim()));
        }

        return requete.OrderBy(v => v.Immatriculation).Select(Projections.Vehicule).PaginerAsync(page, taillePage, ct);
    }

    public async Task<VehiculeDto> ObtenirAsync(Guid id, CancellationToken ct) =>
        await db.Vehicules.Where(v => v.Id == id).Select(Projections.Vehicule).SingleOrDefaultAsync(ct)
            ?? throw new IntrouvableException("Véhicule introuvable.");

    public async Task<VehiculeDto> CreerAsync(VehiculeRequete requete, CancellationToken ct)
    {
        var vehicule = new Vehicule { Immatriculation = "", NumeroChassis = "", Marque = "", Modele = "" };
        await AppliquerAsync(vehicule, requete, ct);
        db.Vehicules.Add(vehicule);
        await db.SaveChangesAsync(ct);
        return await ObtenirAsync(vehicule.Id, ct);
    }

    public async Task<VehiculeDto> ModifierAsync(Guid id, VehiculeRequete requete, CancellationToken ct)
    {
        var vehicule = await db.Vehicules.FindAsync([id], ct)
            ?? throw new IntrouvableException("Véhicule introuvable.");
        await AppliquerAsync(vehicule, requete, ct);
        await db.SaveChangesAsync(ct);
        return await ObtenirAsync(id, ct);
    }

    public async Task<IReadOnlyList<ControleResumeDto>> HistoriqueAsync(Guid id, CancellationToken ct)
    {
        if (!await db.Vehicules.AnyAsync(v => v.Id == id, ct))
            throw new IntrouvableException("Véhicule introuvable.");

        return await db.Controles
            .Where(c => c.VehiculeId == id)
            .OrderByDescending(c => c.DateControle)
            .Select(Projections.ControleResume)
            .ToListAsync(ct);
    }

    private async Task AppliquerAsync(Vehicule vehicule, VehiculeRequete requete, CancellationToken ct)
    {
        var immatriculation = Vehicule.NormaliserImmatriculation(requete.Immatriculation);
        var chassis = Vehicule.NormaliserChassis(requete.NumeroChassis);

        if (await db.Vehicules.AnyAsync(v => v.Immatriculation == immatriculation && v.Id != vehicule.Id, ct))
            throw new ConflitException($"L'immatriculation {immatriculation} est déjà utilisée.");
        if (await db.Vehicules.AnyAsync(v => v.NumeroChassis == chassis && v.Id != vehicule.Id, ct))
            throw new ConflitException($"Le numéro de châssis {chassis} est déjà utilisé.");
        if (!await db.Proprietaires.AnyAsync(p => p.Id == requete.ProprietaireId, ct))
            throw new RegleMetierException("Le propriétaire indiqué n'existe pas.");

        vehicule.Immatriculation = immatriculation;
        vehicule.NumeroChassis = chassis;
        vehicule.Marque = requete.Marque.Trim();
        vehicule.Modele = requete.Modele.Trim();
        vehicule.Annee = requete.Annee;
        vehicule.TypeVehicule = (D.TypeVehicule)requete.TypeVehicule;
        vehicule.Energie = (D.Energie)requete.Energie;
        vehicule.ProprietaireId = requete.ProprietaireId;
    }
}
