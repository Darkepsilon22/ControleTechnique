using CT.Api.Mapping;
using CT.Domain.Entities;
using CT.Domain.Exceptions;
using CT.Infrastructure.Data;
using CT.Infrastructure.Security;
using CT.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using D = CT.Domain.Enums;

namespace CT.Api.Services;

public interface IUtilisateurService
{
    Task<IReadOnlyList<UtilisateurDto>> ListerAsync(CancellationToken ct);
    Task<UtilisateurDto> CreerAsync(CreerUtilisateurRequete requete, CancellationToken ct);
    Task<UtilisateurDto> ModifierAsync(Guid id, ModifierUtilisateurRequete requete, Guid idAdministrateur, CancellationToken ct);
}

public class UtilisateurService(CtDbContext db, IHachageMotDePasse hachage) : IUtilisateurService
{
    public async Task<IReadOnlyList<UtilisateurDto>> ListerAsync(CancellationToken ct) =>
        await db.Utilisateurs
            .OrderBy(u => u.NomComplet)
            .Select(Projections.Utilisateur)
            .ToListAsync(ct);

    public async Task<UtilisateurDto> CreerAsync(CreerUtilisateurRequete requete, CancellationToken ct)
    {
        var email = requete.Email.Trim().ToLowerInvariant();
        await VerifierEmailLibreAsync(email, null, ct);

        var utilisateur = new Utilisateur
        {
            NomComplet = requete.NomComplet.Trim(),
            Email = email,
            MotDePasseHash = hachage.Hacher(requete.MotDePasse),
            Role = (D.Role)requete.Role
        };
        db.Utilisateurs.Add(utilisateur);
        await db.SaveChangesAsync(ct);

        return Projections.UtilisateurVersDto(utilisateur);
    }

    public async Task<UtilisateurDto> ModifierAsync(Guid id, ModifierUtilisateurRequete requete, Guid idAdministrateur, CancellationToken ct)
    {
        var utilisateur = await db.Utilisateurs.FindAsync([id], ct)
            ?? throw new IntrouvableException("Utilisateur introuvable.");

        if (id == idAdministrateur && (!requete.Actif || (D.Role)requete.Role != D.Role.Administrateur))
            throw new RegleMetierException("Vous ne pouvez pas désactiver votre propre compte ni retirer votre rôle d'administrateur.");

        var email = requete.Email.Trim().ToLowerInvariant();
        await VerifierEmailLibreAsync(email, id, ct);

        utilisateur.NomComplet = requete.NomComplet.Trim();
        utilisateur.Email = email;
        utilisateur.Role = (D.Role)requete.Role;
        utilisateur.Actif = requete.Actif;
        if (!string.IsNullOrWhiteSpace(requete.NouveauMotDePasse))
            utilisateur.MotDePasseHash = hachage.Hacher(requete.NouveauMotDePasse);

        await db.SaveChangesAsync(ct);
        return Projections.UtilisateurVersDto(utilisateur);
    }

    private async Task VerifierEmailLibreAsync(string email, Guid? idExclu, CancellationToken ct)
    {
        if (await db.Utilisateurs.AnyAsync(u => u.Email == email && u.Id != idExclu, ct))
            throw new ConflitException($"L'e-mail {email} est déjà utilisé.");
    }
}
