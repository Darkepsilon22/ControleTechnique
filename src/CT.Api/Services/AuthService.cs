using CT.Api.Mapping;
using CT.Api.Securite;
using CT.Infrastructure.Data;
using CT.Infrastructure.Security;
using CT.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CT.Api.Services;

public interface IAuthService
{
    Task<ConnexionReponse?> ConnecterAsync(ConnexionRequete requete, CancellationToken ct);
}

public class AuthService(CtDbContext db, IHachageMotDePasse hachage, IJetonService jetons) : IAuthService
{
    public async Task<ConnexionReponse?> ConnecterAsync(ConnexionRequete requete, CancellationToken ct)
    {
        var email = requete.Email.Trim().ToLowerInvariant();
        var utilisateur = await db.Utilisateurs.SingleOrDefaultAsync(u => u.Email == email, ct);

        if (utilisateur is null || !utilisateur.Actif || !hachage.Verifier(requete.MotDePasse, utilisateur.MotDePasseHash))
            return null;

        var (jeton, expireLe) = jetons.Creer(utilisateur);
        return new ConnexionReponse(jeton, expireLe, Projections.UtilisateurVersDto(utilisateur));
    }
}
