using CT.Api.Mapping;
using CT.Domain.Entities;
using CT.Domain.Exceptions;
using CT.Infrastructure.Data;
using CT.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using D = CT.Domain.Enums;
using S = CT.Shared.Enums;

namespace CT.Api.Services;

public interface IControleService
{
    Task<PageResultat<ControleResumeDto>> ListerAsync(S.StatutControle? statut, string? recherche, Guid? inspecteurId, int page, int taillePage, CancellationToken ct);
    Task<ControleDto> ObtenirAsync(Guid id, CancellationToken ct);
    Task<ControleDto> OuvrirAsync(OuvrirControleRequete requete, Guid inspecteurId, CancellationToken ct);
    Task<ControleDto> OuvrirContreVisiteAsync(Guid controleInitialId, OuvrirContreVisiteRequete requete, Guid inspecteurId, CancellationToken ct);
    Task<ControleDto> SaisirAsync(Guid id, SaisirControleRequete requete, Guid inspecteurId, CancellationToken ct);
    Task<ControleDto> CloturerAsync(Guid id, Guid inspecteurId, CancellationToken ct);
}

public class ControleService(CtDbContext db, TimeProvider horloge) : IControleService
{
    public Task<PageResultat<ControleResumeDto>> ListerAsync(S.StatutControle? statut, string? recherche, Guid? inspecteurId, int page, int taillePage, CancellationToken ct)
    {
        var requete = db.Controles.AsQueryable();
        if (statut is not null)
        {
            var statutDomaine = Enums.Vers<D.StatutControle>(statut.Value);
            requete = requete.Where(c => c.Statut == statutDomaine);
        }
        if (inspecteurId is not null)
            requete = requete.Where(c => c.InspecteurId == inspecteurId);
        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var terme = Vehicule.NormaliserImmatriculation(recherche);
            requete = requete.Where(c => c.Vehicule!.Immatriculation.Contains(terme));
        }

        return requete
            .OrderByDescending(c => c.DateControle)
            .Select(Projections.ControleResume)
            .PaginerAsync(page, taillePage, ct);
    }

    public async Task<ControleDto> ObtenirAsync(Guid id, CancellationToken ct)
    {
        var controle = await ChargerAsync(id, ct);
        return Projections.VersDto(controle, await ChargerCatalogueAsync(ct));
    }

    public async Task<ControleDto> OuvrirAsync(OuvrirControleRequete requete, Guid inspecteurId, CancellationToken ct)
    {
        if (!await db.Vehicules.AnyAsync(v => v.Id == requete.VehiculeId, ct))
            throw new IntrouvableException("Véhicule introuvable.");

        await VerifierAucunControleEnCoursAsync(requete.VehiculeId, ct);

        var maintenant = horloge.GetLocalNow().DateTime;
        var controle = Controle.Ouvrir(requete.VehiculeId, inspecteurId, requete.DateControle ?? maintenant, requete.Kilometrage, maintenant);
        db.Controles.Add(controle);
        await db.SaveChangesAsync(ct);

        return await ObtenirAsync(controle.Id, ct);
    }

    public async Task<ControleDto> OuvrirContreVisiteAsync(Guid controleInitialId, OuvrirContreVisiteRequete requete, Guid inspecteurId, CancellationToken ct)
    {
        var initial = await ChargerAsync(controleInitialId, ct);
        if (initial.ContreVisite is not null)
            throw new ConflitException("Une contre-visite existe déjà pour ce contrôle.");
        await VerifierAucunControleEnCoursAsync(initial.VehiculeId, ct);

        var maintenant = horloge.GetLocalNow().DateTime;
        var contreVisite = Controle.OuvrirContreVisite(
            initial, inspecteurId, requete.DateControle ?? maintenant, requete.Kilometrage, maintenant);
        db.Controles.Add(contreVisite);
        await db.SaveChangesAsync(ct);

        return await ObtenirAsync(contreVisite.Id, ct);
    }

    public async Task<ControleDto> SaisirAsync(Guid id, SaisirControleRequete requete, Guid inspecteurId, CancellationToken ct)
    {
        var controle = await ChargerPourInspecteurAsync(id, inspecteurId, ct);
        controle.VerifierModifiable();

        var catalogue = await ChargerCatalogueAsync(ct);
        controle.Saisir(
            requete.Kilometrage,
            requete.Resultats.Select(r => new SaisiePoint(r.PointControleId, (D.EtatPoint)r.Etat, r.Defaillances, r.Commentaire)).ToList(),
            catalogue);
        await db.SaveChangesAsync(ct);

        return await ObtenirAsync(id, ct);
    }

    public async Task<ControleDto> CloturerAsync(Guid id, Guid inspecteurId, CancellationToken ct)
    {
        var controle = await ChargerPourInspecteurAsync(id, inspecteurId, ct);
        var catalogue = await ChargerCatalogueAsync(ct);

        controle.Cloturer(catalogue, horloge.GetLocalNow().DateTime);
        await db.SaveChangesAsync(ct);

        return Projections.VersDto(controle, catalogue);
    }

    private async Task<List<PointControle>> ChargerCatalogueAsync(CancellationToken ct) =>
        await db.PointsControle.Include(p => p.Fonction).Include(p => p.Defaillances).AsSplitQuery().ToListAsync(ct);

    private async Task VerifierAucunControleEnCoursAsync(Guid vehiculeId, CancellationToken ct)
    {
        if (await db.Controles.AnyAsync(c => c.VehiculeId == vehiculeId && c.Statut == D.StatutControle.Brouillon, ct))
            throw new ConflitException("Un contrôle est déjà en cours pour ce véhicule.");
    }

    private async Task<Controle> ChargerPourInspecteurAsync(Guid id, Guid inspecteurId, CancellationToken ct)
    {
        var controle = await ChargerAsync(id, ct);
        if (controle.InspecteurId != inspecteurId)
            throw new AccesInterditException("Seul l'inspecteur qui a ouvert ce contrôle peut le modifier.");
        return controle;
    }

    private async Task<Controle> ChargerAsync(Guid id, CancellationToken ct) =>
        await db.Controles
            .Include(c => c.Vehicule).ThenInclude(v => v!.Proprietaire)
            .Include(c => c.Inspecteur)
            .Include(c => c.Resultats).ThenInclude(r => r.PointControle).ThenInclude(p => p!.Fonction)
            .Include(c => c.Resultats).ThenInclude(r => r.Defaillances)
            .Include(c => c.ControleInitial).ThenInclude(i => i!.Resultats).ThenInclude(r => r.Defaillances)
            .Include(c => c.ContreVisite)
            .AsSplitQuery()
            .SingleOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new IntrouvableException("Contrôle introuvable.");
}
