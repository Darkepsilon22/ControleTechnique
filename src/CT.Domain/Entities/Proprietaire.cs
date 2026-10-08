namespace CT.Domain.Entities;

public class Proprietaire
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Nom { get; set; }
    public required string Telephone { get; set; }
    public string? Adresse { get; set; }

    public ICollection<Vehicule> Vehicules { get; set; } = [];
}
