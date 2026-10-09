using System.Linq.Expressions;
using CT.Domain.Entities;
using CT.Domain.Rules;
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

    public static Expression<Func<Vehicule, VehiculeDto>> Vehicule(DateOnly aujourdhui) =>
        v => VersVehiculeDto(
            v.Id, v.Immatriculation, v.NumeroChassis, v.Marque, v.Modele, v.DatePremiereImmatriculation,
            v.TypeVehicule, v.Energie, v.ProprietaireId, v.Proprietaire!.Nom,
            v.Controles.Where(c => c.Statut == D.StatutControle.Cloture).OrderByDescending(c => c.DateControle)
                .Select(c => c.Resultat).FirstOrDefault(),
            v.Controles.Where(c => c.Statut == D.StatutControle.Cloture).OrderByDescending(c => c.DateControle)
                .Select(c => c.DateFinValidite).FirstOrDefault(),
            v.Controles.Where(c => c.Statut == D.StatutControle.Cloture).OrderByDescending(c => c.DateControle)
                .Select(c => c.DateLimiteContreVisite).FirstOrDefault(),
            aujourdhui);

    public static readonly Expression<Func<Controle, ControleResumeDto>> ControleResume =
        c => new ControleResumeDto(
            c.Id, c.VehiculeId, c.Vehicule!.Immatriculation, c.Vehicule.Marque + " " + c.Vehicule.Modele,
            c.Inspecteur!.NomComplet, c.DateControle, c.Kilometrage,
            Enums.Vers<S.StatutControle>(c.Statut), Enums.VersNullable<S.ResultatControle>(c.Resultat), c.DateFinValidite,
            c.ControleInitialId != null);

    public static PointControleDto VersDto(PointControle p) => new(
        p.Id, p.Code, p.Libelle, p.FonctionId, p.NumeroFonction, p.Fonction!.Libelle, p.Actif,
        p.Defaillances.OrderBy(d => d.Code).Select(VersDto).ToList());

    public static DefaillanceDto VersDto(Defaillance d) =>
        new(d.Id, d.Code, d.Libelle, (S.NiveauDefaillance)d.Niveau, d.Actif);

    public static ControleDto VersDto(Controle c, IReadOnlyCollection<PointControle> catalogue) => new(
        c.Id,
        c.VehiculeId,
        c.Vehicule!.Immatriculation,
        $"{c.Vehicule.Marque} {c.Vehicule.Modele}",
        c.Vehicule.NumeroChassis,
        (S.TypeVehicule)c.Vehicule.TypeVehicule,
        (S.Energie)c.Vehicule.Energie,
        c.Vehicule.DatePremiereImmatriculation,
        c.Vehicule.Proprietaire!.Nom,
        c.InspecteurId,
        c.Inspecteur!.NomComplet,
        c.DateControle,
        c.DateControlePeriodique,
        c.Kilometrage,
        (S.StatutControle)c.Statut,
        (S.ResultatControle?)c.Resultat,
        c.DateFinValidite,
        c.DateLimiteContreVisite,
        c.ClotureLe,
        c.Resultats
            .OrderBy(r => CleTri(r.PointControle!.Code))
            .Select(r => new ResultatPointDto(
                r.PointControleId,
                r.PointControle!.Code,
                r.PointControle.Libelle,
                r.PointControle.NumeroFonction,
                r.PointControle.Fonction!.Libelle,
                (S.EtatPoint)r.Etat,
                r.Commentaire,
                r.Defaillances.OrderBy(d => d.Code).Select(VersDto).ToList()))
            .ToList(),
        c.PointsASaisir(catalogue).Select(p => p.Id).ToList(),
        c.ControleInitialId,
        c.ContreVisite?.Id);

    public static string CleTri(string code) =>
        string.Join('.', code.Split('.').Select(s => int.TryParse(s, out var n) ? n.ToString("D2") : s));

    private static VehiculeDto VersVehiculeDto(Guid id, string immatriculation, string chassis, string marque, string modele,
        DateOnly premiereImmatriculation, D.TypeVehicule type, D.Energie energie, Guid proprietaireId, string proprietaireNom,
        D.ResultatControle? dernierResultat, DateOnly? finValidite, DateOnly? limiteContreVisite, DateOnly aujourdhui)
    {
        var echeance = EcheanceControle.Calculer(premiereImmatriculation, dernierResultat, finValidite, limiteContreVisite, aujourdhui);
        return new VehiculeDto(id, immatriculation, chassis, marque, modele, premiereImmatriculation,
            (S.TypeVehicule)type, (S.Energie)energie, proprietaireId, proprietaireNom,
            echeance.DateLimite, (S.StatutEcheance)echeance.Statut);
    }
}
