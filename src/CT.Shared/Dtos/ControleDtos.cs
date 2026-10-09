using System.ComponentModel.DataAnnotations;
using CT.Shared.Enums;

namespace CT.Shared.Dtos;

public record DefaillanceDto(Guid Id, string Code, string Libelle, NiveauDefaillance Niveau, bool Actif);

public record PointControleDto(
    Guid Id,
    string Code,
    string Libelle,
    Guid FonctionId,
    int NumeroFonction,
    string Fonction,
    bool Actif,
    IReadOnlyList<DefaillanceDto> Defaillances);

public record PointControleRequete(
    Guid? Id,
    Guid FonctionId,
    [Required, StringLength(12, MinimumLength = 5)] string Code,
    [Required, StringLength(150, MinimumLength = 3)] string Libelle,
    bool Actif = true);

public record DefaillanceRequete(
    Guid? Id,
    [Required, StringLength(16, MinimumLength = 9)] string Code,
    [Required, StringLength(250, MinimumLength = 3)] string Libelle,
    bool Actif = true);

public record OuvrirControleRequete(
    Guid VehiculeId,
    [Range(0, 3_000_000)] int Kilometrage,
    DateTime? DateControle = null);

public record OuvrirContreVisiteRequete(
    [Range(0, 3_000_000)] int Kilometrage,
    DateTime? DateControle = null);

public record SaisiePointRequete(
    Guid PointControleId,
    EtatPoint Etat,
    IReadOnlyList<Guid> Defaillances,
    [StringLength(500)] string? Commentaire);

public record SaisirControleRequete(
    [Range(0, 3_000_000)] int? Kilometrage,
    [Required] IReadOnlyList<SaisiePointRequete> Resultats);

public record ResultatPointDto(
    Guid PointControleId,
    string Code,
    string Libelle,
    int NumeroFonction,
    string Fonction,
    EtatPoint Etat,
    string? Commentaire,
    IReadOnlyList<DefaillanceDto> Defaillances);

public record ControleResumeDto(
    Guid Id,
    Guid VehiculeId,
    string Immatriculation,
    string Vehicule,
    string InspecteurNom,
    DateTime DateControle,
    int Kilometrage,
    StatutControle Statut,
    ResultatControle? Resultat,
    DateOnly? DateFinValidite,
    bool EstContreVisite);

public record ControleDto(
    Guid Id,
    Guid VehiculeId,
    string Immatriculation,
    string Vehicule,
    string NumeroChassis,
    TypeVehicule TypeVehicule,
    Energie Energie,
    DateOnly DatePremiereImmatriculation,
    string ProprietaireNom,
    Guid InspecteurId,
    string InspecteurNom,
    DateTime DateControle,
    DateTime DateControlePeriodique,
    int Kilometrage,
    StatutControle Statut,
    ResultatControle? Resultat,
    DateOnly? DateFinValidite,
    DateOnly? DateLimiteContreVisite,
    DateTime? ClotureLe,
    IReadOnlyList<ResultatPointDto> Resultats,
    IReadOnlyList<Guid> PointsASaisir,
    Guid? ControleInitialId,
    Guid? ContreVisiteId)
{
    public bool EstContreVisite => ControleInitialId is not null;

    public IEnumerable<DefaillanceDto> DefaillancesConstatees => Resultats.SelectMany(r => r.Defaillances);
}
