using CT.Shared.Dtos;
using CT.Shared.Enums;

namespace CT.Desktop.Services;

public class Session
{
    public string? Jeton { get; private set; }
    public UtilisateurDto? Utilisateur { get; private set; }

    public bool EstAdministrateur => Utilisateur?.Role == Role.Administrateur;
    public bool EstInspecteur => Utilisateur?.Role == Role.Inspecteur;
    public bool EstReception => Utilisateur?.Role == Role.Reception;
    public bool PeutGererVehicules => EstAdministrateur || EstReception;

    public void Ouvrir(ConnexionReponse connexion)
    {
        Jeton = connexion.Jeton;
        Utilisateur = connexion.Utilisateur;
    }

    public void Fermer()
    {
        Jeton = null;
        Utilisateur = null;
    }
}
