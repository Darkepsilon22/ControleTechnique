using CT.Domain.Enums;

namespace CT.Domain.Entities;

public class PointControle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategorieId { get; set; }
    public CategoriePoint? Categorie { get; set; }
    public required string Libelle { get; set; }
    public Gravite Gravite { get; set; }

    public bool Actif { get; set; } = true;
}
