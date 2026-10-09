using System.ComponentModel.DataAnnotations;
using CT.Shared.Enums;

namespace CT.Shared.Dtos;

public record ProprietaireDto(Guid Id, string Nom, string Telephone, string? Adresse, int NombreVehicules);

public record ProprietaireRequete(
    [Required, StringLength(100, MinimumLength = 2)] string Nom,
    [Required, StringLength(30, MinimumLength = 6)] string Telephone,
    [StringLength(250)] string? Adresse);

public record VehiculeDto(
    Guid Id,
    string Immatriculation,
    string NumeroChassis,
    string Marque,
    string Modele,
    DateOnly DatePremiereImmatriculation,
    TypeVehicule TypeVehicule,
    Energie Energie,
    Guid ProprietaireId,
    string ProprietaireNom,
    DateOnly DateLimiteControle,
    StatutEcheance Echeance);

public record VehiculeRequete(
    [Required, StringLength(20, MinimumLength = 4)] string Immatriculation,
    [Required, StringLength(17, MinimumLength = 17, ErrorMessage = "Le numéro de châssis (VIN) doit contenir 17 caractères.")] string NumeroChassis,
    [Required, StringLength(50)] string Marque,
    [Required, StringLength(50)] string Modele,
    DateOnly DatePremiereImmatriculation,
    TypeVehicule TypeVehicule,
    Energie Energie,
    Guid ProprietaireId);
