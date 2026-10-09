using CT.Domain.Enums;
using CT.Domain.Exceptions;
using CT.Domain.Rules;

namespace CT.Domain.Entities;

public record SaisiePoint(Guid PointControleId, EtatPoint Etat, IReadOnlyCollection<Guid> Defaillances, string? Commentaire);

public class Controle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehiculeId { get; set; }
    public Vehicule? Vehicule { get; set; }
    public Guid InspecteurId { get; set; }
    public Utilisateur? Inspecteur { get; set; }
    public DateTime DateControle { get; set; }
    public DateTime DateControlePeriodique { get; set; }
    public int Kilometrage { get; set; }
    public StatutControle Statut { get; set; } = StatutControle.Brouillon;
    public ResultatControle? Resultat { get; set; }
    public DateOnly? DateFinValidite { get; set; }
    public DateOnly? DateLimiteContreVisite { get; set; }
    public DateTime CreeLe { get; set; }
    public DateTime? ClotureLe { get; set; }
    public DateTime? SynchroniseLe { get; set; }

    public Guid? ControleInitialId { get; set; }
    public Controle? ControleInitial { get; set; }
    public Controle? ContreVisite { get; set; }

    public List<ResultatPoint> Resultats { get; set; } = [];

    public bool EstCloture => Statut == StatutControle.Cloture;
    public bool EstContreVisite => ControleInitialId is not null;

    public static Controle Ouvrir(Guid vehiculeId, Guid inspecteurId, DateTime dateControle, int kilometrage, DateTime maintenant)
    {
        VerifierKilometrage(kilometrage);
        if (dateControle > maintenant.AddDays(1))
            throw new RegleMetierException("La date du contrôle ne peut pas être dans le futur.");

        return new Controle
        {
            VehiculeId = vehiculeId,
            InspecteurId = inspecteurId,
            DateControle = dateControle,
            DateControlePeriodique = dateControle,
            Kilometrage = kilometrage,
            CreeLe = maintenant
        };
    }

    public static Controle OuvrirContreVisite(Controle precedent, Guid inspecteurId, DateTime dateControle, int kilometrage, DateTime maintenant)
    {
        if (precedent.Statut != StatutControle.Cloture || precedent.Resultat is null or ResultatControle.Favorable)
            throw new RegleMetierException("Une contre-visite ne peut suivre qu'un contrôle clôturé défavorable.");
        if (dateControle < precedent.DateControle)
            throw new RegleMetierException("La contre-visite ne peut pas précéder le contrôle qu'elle vérifie.");
        if (DateOnly.FromDateTime(dateControle) > precedent.DateLimiteContreVisite)
            throw new RegleMetierException(
                $"Le délai de contre-visite ({Reglementation.DelaiContreVisiteMois} mois après le contrôle périodique) est dépassé : "
                + "un nouveau contrôle technique périodique est nécessaire.");

        var contreVisite = Ouvrir(precedent.VehiculeId, inspecteurId, dateControle, kilometrage, maintenant);
        contreVisite.DateControlePeriodique = precedent.DateControlePeriodique;
        contreVisite.ControleInitialId = precedent.Id;
        contreVisite.ControleInitial = precedent;
        return contreVisite;
    }

    public IReadOnlyList<PointControle> PointsASaisir(IReadOnlyCollection<PointControle> catalogue)
    {
        var energie = (Vehicule ?? throw new InvalidOperationException("Le véhicule du contrôle doit être chargé.")).Energie;
        var applicables = catalogue.Where(p => p.Actif && p.EstApplicable(energie)).ToList();
        if (!EstContreVisite)
            return applicables;

        var precedent = ControleInitial
            ?? throw new InvalidOperationException("Le contrôle précédent doit être chargé pour une contre-visite.");
        var codes = RegleContreVisite.PointsAReverifier(
            RegleContreVisite.CodesMotivantUneContreVisite(precedent),
            applicables.Select(p => p.Code).ToList());
        return applicables.Where(p => codes.Contains(p.Code)).ToList();
    }

    public void Saisir(int? kilometrage, IReadOnlyCollection<SaisiePoint> saisies, IReadOnlyCollection<PointControle> catalogue)
    {
        VerifierModifiable();

        if (saisies.GroupBy(s => s.PointControleId).Any(g => g.Count() > 1))
            throw new RegleMetierException("Un point de contrôle ne peut être saisi qu'une seule fois.");

        var aSaisir = PointsASaisir(catalogue).ToDictionary(p => p.Id);
        foreach (var saisie in saisies)
        {
            if (!aSaisir.TryGetValue(saisie.PointControleId, out var point))
                throw new RegleMetierException(EstContreVisite
                    ? "Ce point n'est pas à revérifier lors de cette contre-visite."
                    : "Ce point de contrôle est inconnu, désactivé ou non applicable à ce véhicule.");
            VerifierDefaillances(point, saisie);
        }

        if (kilometrage is not null)
        {
            VerifierKilometrage(kilometrage.Value);
            Kilometrage = kilometrage.Value;
        }

        foreach (var saisie in saisies)
        {
            var point = aSaisir[saisie.PointControleId];
            var resultat = Resultats.FirstOrDefault(r => r.PointControleId == point.Id);
            if (resultat is null)
            {
                resultat = new ResultatPoint { ControleId = Id, PointControleId = point.Id, PointControle = point };
                Resultats.Add(resultat);
            }

            resultat.Etat = saisie.Etat;
            resultat.Commentaire = string.IsNullOrWhiteSpace(saisie.Commentaire) ? null : saisie.Commentaire.Trim();
            resultat.Defaillances.Clear();
            resultat.Defaillances.AddRange(point.Defaillances.Where(d => saisie.Defaillances.Contains(d.Id)));
        }
    }

    public void Cloturer(IReadOnlyCollection<PointControle> catalogue, DateTime maintenant)
    {
        VerifierModifiable();

        var saisis = Resultats.Select(r => r.PointControleId).ToHashSet();
        var manquants = PointsASaisir(catalogue).Count(p => !saisis.Contains(p.Id));
        if (manquants > 0)
            throw new RegleMetierException($"{manquants} point(s) de contrôle n'ont pas encore été saisis.");

        var resultat = Reglementation.Resultat(Resultats.SelectMany(r => r.Defaillances).Select(d => d.Niveau));
        var dateControlePeriodique = DateOnly.FromDateTime(DateControlePeriodique);

        Resultat = resultat;
        DateFinValidite = Reglementation.FinValidite(resultat, DateOnly.FromDateTime(DateControle), dateControlePeriodique);
        DateLimiteContreVisite = resultat == ResultatControle.Favorable
            ? null
            : Reglementation.LimiteContreVisite(dateControlePeriodique);
        Statut = StatutControle.Cloture;
        ClotureLe = maintenant;
    }

    public void VerifierModifiable()
    {
        if (EstCloture)
            throw new ControleClotureException();
    }

    private static void VerifierDefaillances(PointControle point, SaisiePoint saisie)
    {
        if (saisie.Etat == EtatPoint.NonConforme && saisie.Defaillances.Count == 0)
            throw new RegleMetierException($"Point {point.Code} : indiquez au moins une défaillance constatée.");
        if (saisie.Etat != EtatPoint.NonConforme && saisie.Defaillances.Count > 0)
            throw new RegleMetierException($"Point {point.Code} : un point sans défaillance ne peut pas en comporter.");

        var possibles = point.Defaillances.Where(d => d.Actif).Select(d => d.Id).ToHashSet();
        if (saisie.Defaillances.Any(id => !possibles.Contains(id)))
            throw new RegleMetierException($"Point {point.Code} : une défaillance indiquée n'appartient pas à ce point.");
    }

    private static void VerifierKilometrage(int kilometrage)
    {
        if (kilometrage < 0)
            throw new RegleMetierException("Le kilométrage ne peut pas être négatif.");
    }
}
