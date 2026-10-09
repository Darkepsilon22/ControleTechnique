using CT.Domain.Entities;
using CT.Domain.Enums;

namespace CT.Tests.Domain;

public class CatalogueDeTest
{
    public static readonly DateTime DateControle = new(2026, 9, 1, 10, 0, 0);

    public PointControle Plaques { get; } = Point("0.1.1", "0.1.1.a.2");
    public PointControle Plaquettes { get; } = Point("1.1.13", "1.1.13.a.1", "1.1.13.a.2", "1.1.13.a.3");
    public PointControle FreinService { get; } = Point("1.2.2", "1.2.2.a.2", "1.2.2.a.3");
    public PointControle EssuieGlace { get; } = Point("3.4.1", "3.4.1.a.2", "3.4.1.b.1");
    public PointControle FeuxStop { get; } = Point("4.3.1", "4.3.1.a.1", "4.3.1.a.2");
    public PointControle Clignotants { get; } = Point("4.4.1", "4.4.1.a.1");
    public PointControle Amortisseurs { get; } = Point("5.3.2", "5.3.2.a.2");
    public PointControle Compteur { get; } = Point("7.11.1", "7.11.1.a.1");
    public PointControle EmissionsEssence { get; } = Point("8.2.12", "8.2.12.a.2");
    public PointControle Opacite { get; } = Point("8.2.22", "8.2.22.a.2");

    public IReadOnlyList<PointControle> Points =>
        [Plaques, Plaquettes, FreinService, EssuieGlace, FeuxStop, Clignotants, Amortisseurs, Compteur, EmissionsEssence, Opacite];

    public static PointControle Point(string code, params string[] defaillances)
    {
        var point = new PointControle { Code = code, Libelle = $"Point {code}" };
        point.Defaillances.AddRange(defaillances.Select(d => new Defaillance
        {
            PointControleId = point.Id,
            PointControle = point,
            Code = d,
            Libelle = $"Défaillance {d}",
            Niveau = Defaillance.NiveauDepuisCode(d, code)
        }));
        return point;
    }

    public static Defaillance D(PointControle point, string code) => point.Defaillances.Single(d => d.Code == code);

    public Controle NouveauControle(Energie energie = Energie.Essence, DateTime? date = null)
    {
        var dateControle = date ?? DateControle;
        var controle = Controle.Ouvrir(Guid.NewGuid(), Guid.NewGuid(), dateControle, 50_000, dateControle);
        controle.Vehicule = Vehicule(energie);
        return controle;
    }

    public static Vehicule Vehicule(Energie energie) => new()
    {
        Immatriculation = "AB-123-CD",
        NumeroChassis = "VF1AB000000000001",
        Marque = "Renault",
        Modele = "Clio",
        DatePremiereImmatriculation = new DateOnly(2018, 5, 1),
        Energie = energie
    };

    public List<SaisiePoint> ToutConforme(Controle controle, params Defaillance[] defaillances) =>
        controle.PointsASaisir(Points).Select(p =>
        {
            var constatees = defaillances.Where(d => d.PointControleId == p.Id).Select(d => d.Id).ToList();
            return constatees.Count > 0
                ? new SaisiePoint(p.Id, EtatPoint.NonConforme, constatees, null)
                : new SaisiePoint(p.Id, EtatPoint.Conforme, [], null);
        }).ToList();

    public Controle ControleCloture(Energie energie, params Defaillance[] defaillances)
    {
        var controle = NouveauControle(energie);
        controle.Saisir(null, ToutConforme(controle, defaillances), Points);
        controle.Cloturer(Points, DateControle.AddMinutes(40));
        return controle;
    }
}
