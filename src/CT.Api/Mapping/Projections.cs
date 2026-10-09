using System.Linq.Expressions;
using CT.Domain.Entities;
using CT.Shared.Dtos;
using D = CT.Domain.Enums;
using S = CT.Shared.Enums;

namespace CT.Api.Mapping;

public static class Projections
{
    public static readonly Expression<Func<Utilisateur, UtilisateurDto>> Utilisateur =
        u => new UtilisateurDto(u.Id, u.NomComplet, u.Email, Enums.Vers<S.Role>(u.Role), u.Actif);

    public static readonly Func<Utilisateur, UtilisateurDto> UtilisateurVersDto = Utilisateur.Compile();

    public static readonly Expression<Func<Proprietaire, ProprietaireDto>> Proprietaire =
        p => new ProprietaireDto(p.Id, p.Nom, p.Telephone, p.Adresse, p.Vehicules.Count);

    public static readonly Expression<Func<Vehicule, VehiculeDto>> Vehicule =
        v => new VehiculeDto(
            v.Id, v.Immatriculation, v.NumeroChassis, v.Marque, v.Modele, v.Annee,
            Enums.Vers<S.TypeVehicule>(v.TypeVehicule), Enums.Vers<S.Energie>(v.Energie),
            v.ProprietaireId, v.Proprietaire!.Nom,
            v.Controles
                .Where(c => c.Statut == D.StatutControle.Cloture)
                .OrderByDescending(c => c.DateControle)
                .Select(c => c.DateFinValidite)
                .FirstOrDefault());

    public static readonly Expression<Func<PointControle, PointControleDto>> PointControle =
        p => new PointControleDto(p.Id, p.CategorieId, p.Categorie!.Libelle, p.Libelle, Enums.Vers<S.Gravite>(p.Gravite), p.Actif);

    public static readonly Expression<Func<Controle, ControleResumeDto>> ControleResume =
        c => new ControleResumeDto(
            c.Id, c.VehiculeId, c.Vehicule!.Immatriculation, c.Vehicule.Marque + " " + c.Vehicule.Modele,
            c.Inspecteur!.NomComplet, c.DateControle, c.Kilometrage,
            Enums.Vers<S.StatutControle>(c.Statut), Enums.VersNullable<S.ResultatControle>(c.Resultat), c.DateFinValidite,
            c.ControleInitialId != null);

    public static ControleDto VersDto(Controle c) => new(
        c.Id,
        c.VehiculeId,
        c.Vehicule!.Immatriculation,
        $"{c.Vehicule.Marque} {c.Vehicule.Modele}",
        c.Vehicule.Proprietaire!.Nom,
        c.InspecteurId,
        c.Inspecteur!.NomComplet,
        c.DateControle,
        c.Kilometrage,
        (S.StatutControle)c.Statut,
        (S.ResultatControle?)c.Resultat,
        c.Observations,
        c.DateFinValidite,
        c.ClotureLe,
        c.Resultats
            .OrderBy(r => r.PointControle!.Categorie!.Libelle)
            .ThenByDescending(r => r.PointControle!.Gravite)
            .ThenBy(r => r.PointControle!.Libelle)
            .Select(r => new ResultatPointDto(
                r.PointControleId,
                r.PointControle!.Categorie!.Libelle,
                r.PointControle.Libelle,
                (S.Gravite)r.PointControle.Gravite,
                (S.EtatPoint)r.Etat,
                r.Commentaire))
            .ToList(),
        c.ControleInitialId,
        c.ControleInitial?.DateControle,
        c.EstContreVisite ? c.PointsAVerifier([]).ToList() : [],
        c.ContreVisite?.Id);
}
