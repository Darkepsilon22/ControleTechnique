using System.Security.Claims;
using CT.Domain.Enums;

namespace CT.Api.Securite;

public static class Roles
{
    public const string Administrateur = nameof(Role.Administrateur);
    public const string Inspecteur = nameof(Role.Inspecteur);
    public const string Reception = nameof(Role.Reception);
    public const string AdministrateurOuReception = Administrateur + "," + Reception;
}

public static class ClaimsPrincipalExtensions
{
    public static Guid IdUtilisateur(this ClaimsPrincipal utilisateur) =>
        Guid.Parse(utilisateur.FindFirstValue(JetonService.ClaimId)
            ?? throw new InvalidOperationException("Jeton sans identifiant d'utilisateur."));
}
