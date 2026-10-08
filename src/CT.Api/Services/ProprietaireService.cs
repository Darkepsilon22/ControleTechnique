using CT.Api.Mapping;
using CT.Domain.Entities;
using CT.Domain.Exceptions;
using CT.Infrastructure.Data;
using CT.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CT.Api.Services;

public interface IProprietaireService
{
    Task<PageResultat<ProprietaireDto>> RechercherAsync(string? recherche, int page, int taillePage, CancellationToken ct);
    Task<ProprietaireDto> CreerAsync(ProprietaireRequete requete, CancellationToken ct);
    Task<ProprietaireDto> ModifierAsync(Guid id, ProprietaireRequete requete, CancellationToken ct);
}

public class ProprietaireService(CtDbContext db) : IProprietaireService
{
    public Task<PageResultat<ProprietaireDto>> RechercherAsync(string? recherche, int page, int taillePage, CancellationToken ct)
    {
        var requete = db.Proprietaires.AsQueryable();
        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var terme = recherche.Trim();
            requete = requete.Where(p => p.Nom.Contains(terme) || p.Telephone.Contains(terme));
        }

        return requete.OrderBy(p => p.Nom).Select(Projections.Proprietaire).PaginerAsync(page, taillePage, ct);
    }

    public async Task<ProprietaireDto> CreerAsync(ProprietaireRequete requete, CancellationToken ct)
    {
        var proprietaire = new Proprietaire { Nom = "", Telephone = "" };
        Appliquer(proprietaire, requete);
        db.Proprietaires.Add(proprietaire);
        await db.SaveChangesAsync(ct);
        return await ObtenirAsync(proprietaire.Id, ct);
    }

    public async Task<ProprietaireDto> ModifierAsync(Guid id, ProprietaireRequete requete, CancellationToken ct)
    {
        var proprietaire = await db.Proprietaires.FindAsync([id], ct)
            ?? throw new IntrouvableException("Propriétaire introuvable.");
        Appliquer(proprietaire, requete);
        await db.SaveChangesAsync(ct);
        return await ObtenirAsync(id, ct);
    }

    private Task<ProprietaireDto> ObtenirAsync(Guid id, CancellationToken ct) =>
        db.Proprietaires.Where(p => p.Id == id).Select(Projections.Proprietaire).SingleAsync(ct);

    private static void Appliquer(Proprietaire proprietaire, ProprietaireRequete requete)
    {
        proprietaire.Nom = requete.Nom.Trim();
        proprietaire.Telephone = requete.Telephone.Trim();
        proprietaire.Adresse = string.IsNullOrWhiteSpace(requete.Adresse) ? null : requete.Adresse.Trim();
    }
}
