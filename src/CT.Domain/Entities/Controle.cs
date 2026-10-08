using CT.Domain.Enums;
using CT.Domain.Exceptions;
using CT.Domain.Rules;

namespace CT.Domain.Entities;

public record SaisiePoint(Guid PointControleId, EtatPoint Etat, string? Commentaire);

public class Controle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehiculeId { get; set; }
    public Vehicule? Vehicule { get; set; }
    public Guid InspecteurId { get; set; }
    public Utilisateur? Inspecteur { get; set; }
    public DateTime DateControle { get; set; }
    public int Kilometrage { get; set; }
    public StatutControle Statut { get; set; } = StatutControle.Brouillon;
    public ResultatControle? Resultat { get; set; }
    public string? Observations { get; set; }
    public DateOnly? DateFinValidite { get; set; }
    public DateTime CreeLe { get; set; }
    public DateTime? ClotureLe { get; set; }
    public DateTime? SynchroniseLe { get; set; }

    public List<ResultatPoint> Resultats { get; set; } = [];

    public bool EstCloture => Statut == StatutControle.Cloture;

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
            Kilometrage = kilometrage,
            CreeLe = maintenant
        };
    }

    public void Saisir(int? kilometrage, IReadOnlyCollection<SaisiePoint> saisies)
    {
        VerifierModifiable();

        var doublon = saisies.GroupBy(s => s.PointControleId).FirstOrDefault(g => g.Count() > 1);
        if (doublon is not null)
            throw new RegleMetierException("Un point de contrôle ne peut être saisi qu'une seule fois.");

        if (kilometrage is not null)
        {
            VerifierKilometrage(kilometrage.Value);
            Kilometrage = kilometrage.Value;
        }

        foreach (var saisie in saisies)
        {
            var commentaire = string.IsNullOrWhiteSpace(saisie.Commentaire) ? null : saisie.Commentaire.Trim();
            var existant = Resultats.FirstOrDefault(r => r.PointControleId == saisie.PointControleId);
            if (existant is null)
            {
                Resultats.Add(new ResultatPoint
                {
                    ControleId = Id,
                    PointControleId = saisie.PointControleId,
                    Etat = saisie.Etat,
                    Commentaire = commentaire
                });
            }
            else
            {
                existant.Etat = saisie.Etat;
                existant.Commentaire = commentaire;
            }
        }
    }

    public void Cloturer(IEnumerable<Guid> pointsActifsIds, int dureeValiditeMois, DateTime maintenant)
    {
        VerifierModifiable();

        var saisis = Resultats.Select(r => r.PointControleId).ToHashSet();
        var manquants = pointsActifsIds.Count(id => !saisis.Contains(id));
        if (manquants > 0)
            throw new RegleMetierException($"{manquants} point(s) de contrôle n'ont pas encore été saisis.");

        var calcul = CalculateurResultat.Calculer(Resultats.Select(r =>
        {
            var point = r.PointControle
                ?? throw new InvalidOperationException("Le point de contrôle de chaque résultat doit être chargé.");
            return new PointEvalue(point.Libelle, point.Gravite, r.Etat);
        }));

        Resultat = calcul.Resultat;
        Observations = calcul.Observations.Count > 0 ? string.Join(Environment.NewLine, calcul.Observations) : null;
        DateFinValidite = CalculateurResultat.CalculerFinValidite(calcul.Resultat, DateControle, dureeValiditeMois);
        Statut = StatutControle.Cloture;
        ClotureLe = maintenant;
    }

    public void VerifierModifiable()
    {
        if (EstCloture)
            throw new ControleClotureException();
    }

    private static void VerifierKilometrage(int kilometrage)
    {
        if (kilometrage < 0)
            throw new RegleMetierException("Le kilométrage ne peut pas être négatif.");
    }
}
