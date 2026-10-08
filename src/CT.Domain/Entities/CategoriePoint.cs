namespace CT.Domain.Entities;

public class CategoriePoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Libelle { get; set; }

    public ICollection<PointControle> Points { get; set; } = [];
}
