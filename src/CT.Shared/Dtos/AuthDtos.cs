using System.ComponentModel.DataAnnotations;
using CT.Shared.Enums;

namespace CT.Shared.Dtos;

public record ConnexionRequete(
    [Required, EmailAddress] string Email,
    [Required] string MotDePasse);

public record ConnexionReponse(string Jeton, DateTime ExpireLe, UtilisateurDto Utilisateur);

public record UtilisateurDto(Guid Id, string NomComplet, string Email, Role Role, bool Actif);

public record CreerUtilisateurRequete(
    [Required, StringLength(100, MinimumLength = 2)] string NomComplet,
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string MotDePasse,
    Role Role);

public record ModifierUtilisateurRequete(
    [Required, StringLength(100, MinimumLength = 2)] string NomComplet,
    [Required, EmailAddress, StringLength(150)] string Email,
    Role Role,
    bool Actif,
    [StringLength(100, MinimumLength = 8)] string? NouveauMotDePasse = null);
