using CT.Domain.Enums;

namespace CT.Domain.Entities;

public class Vehicule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Immatriculation { get; set; }
    public required string NumeroChassis { get; set; }
    public required string Marque { get; set; }
    public required string Modele { get; set; }
    public DateOnly DatePremiereImmatriculation { get; set; }
    public TypeVehicule TypeVehicule { get; set; }
    public Energie Energie { get; set; }

    public Guid ProprietaireId { get; set; }
    public Proprietaire? Proprietaire { get; set; }

    public ICollection<Controle> Controles { get; set; } = [];

    public static string NormaliserImmatriculation(string immatriculation) =>
        immatriculation.Trim().Replace(" ", "").ToUpperInvariant();

    public static string NormaliserChassis(string numeroChassis) =>
        numeroChassis.Trim().Replace(" ", "").ToUpperInvariant();
}
