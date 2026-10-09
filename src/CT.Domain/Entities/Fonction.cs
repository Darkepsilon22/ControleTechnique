namespace CT.Domain.Entities;

public class Fonction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Numero { get; set; }
    public required string Libelle { get; set; }

    public ICollection<PointControle> Points { get; set; } = [];
}
