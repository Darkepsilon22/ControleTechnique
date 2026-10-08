using CT.Domain.Enums;

namespace CT.Domain.Entities;

public class Utilisateur
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string NomComplet { get; set; }
    public required string Email { get; set; }
    public required string MotDePasseHash { get; set; }
    public Role Role { get; set; }
    public bool Actif { get; set; } = true;

    public ICollection<Controle> Controles { get; set; } = [];
}
