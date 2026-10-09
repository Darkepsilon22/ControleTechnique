using System.ComponentModel.DataAnnotations;
using CT.Shared.Enums;

namespace CT.Shared.Dtos;

public record PointControleDto(Guid Id, Guid CategorieId, string Categorie, string Libelle, Gravite Gravite, bool Actif);

public record PointControleRequete(
    Guid? Id,
    Guid CategorieId,
    [Required, StringLength(150, MinimumLength = 3)] string Libelle,
    Gravite Gravite,
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
    [StringLength(500)] string? Commentaire);

public record SaisirControleRequete(
    [Range(0, 3_000_000)] int? Kilometrage,
    [Required] IReadOnlyList<SaisiePointRequete> Resultats);

public record ResultatPointDto(
    Guid PointControleId,
    string Categorie,
    string Libelle,
    Gravite Gravite,
    EtatPoint Etat,
    string? Commentaire);

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
    string ProprietaireNom,
    Guid InspecteurId,
    string InspecteurNom,
    DateTime DateControle,
    int Kilometrage,
    StatutControle Statut,
    ResultatControle? Resultat,
    string? Observations,
    DateOnly? DateFinValidite,
    DateTime? ClotureLe,
    IReadOnlyList<ResultatPointDto> Resultats,
    Guid? ControleInitialId,
    DateTime? DateControleInitial,
    IReadOnlyList<Guid> PointsContreVisite,
    Guid? ContreVisiteId)
{
    public bool EstContreVisite => ControleInitialId is not null;
}
