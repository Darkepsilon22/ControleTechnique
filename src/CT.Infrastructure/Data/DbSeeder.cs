using CT.Domain.Entities;
using CT.Domain.Enums;
using CT.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CT.Infrastructure.Data;

public static class DbSeeder
{
    public const string EmailAdmin = "admin@ct.local";
    public const string EmailInspecteur = "inspecteur@ct.local";
    public const string EmailReception = "reception@ct.local";
    public const string MotDePasseDemo = "Demo123!";

    private const int NombreVehicules = 200;
    private const int NombreControlesHistorique = 150;

    private static readonly (string Marque, string[] Modeles, string[] Utilitaires)[] Catalogue =
    [
        ("Renault", ["Clio", "Mégane", "Captur"], ["Kangoo", "Master"]),
        ("Peugeot", ["208", "308", "3008"], ["Partner", "Expert"]),
        ("Citroën", ["C3", "C4", "C5 Aircross"], ["Berlingo", "Jumper"]),
        ("Toyota", ["Yaris", "Corolla", "C-HR"], ["Hilux", "Proace"]),
        ("Volkswagen", ["Polo", "Golf", "Tiguan"], ["Transporter", "Caddy"]),
        ("Ford", ["Fiesta", "Focus", "Puma"], ["Transit", "Ranger"])
    ];

    private static readonly string[] Prenoms =
        ["Jean", "Marie", "Paul", "Sophie", "Luc", "Claire", "Hugo", "Emma", "Louis", "Alice", "Nicolas", "Julie", "Thomas", "Laura", "Marc"];

    private static readonly string[] Noms =
        ["Martin", "Bernard", "Dubois", "Durand", "Lefebvre", "Moreau", "Laurent", "Simon", "Michel", "Garcia", "Roux", "Fournier", "Girard", "Bonnet", "Mercier"];

    private static readonly string[] Villes = ["Lyon", "Nantes", "Lille", "Rennes", "Dijon", "Tours", "Angers", "Nancy"];

    public static async Task SeedAsync(CtDbContext db, IHachageMotDePasse hachage, TimeProvider horloge, CancellationToken ct = default)
    {
        var maintenant = horloge.GetLocalNow().DateTime;
        var aleatoire = new Random(42);

        if (!await db.Utilisateurs.AnyAsync(ct))
            AjouterUtilisateurs(db, hachage);
        if (!await db.Fonctions.AnyAsync(ct))
            AjouterCatalogue(db);
        if (!await db.Proprietaires.AnyAsync(ct))
            AjouterProprietairesEtVehicules(db, aleatoire, maintenant);
        await db.SaveChangesAsync(ct);

        if (!await db.Controles.AnyAsync(ct))
        {
            await AjouterHistoriqueAsync(db, aleatoire, maintenant, ct);
            await db.SaveChangesAsync(ct);
        }
    }

    private static void AjouterUtilisateurs(CtDbContext db, IHachageMotDePasse hachage)
    {
        var hash = hachage.Hacher(MotDePasseDemo);
        db.Utilisateurs.AddRange(
            new Utilisateur { NomComplet = "Alice Admin", Email = EmailAdmin, MotDePasseHash = hash, Role = Role.Administrateur },
            new Utilisateur { NomComplet = "Ivan Inspecteur", Email = EmailInspecteur, MotDePasseHash = hash, Role = Role.Inspecteur },
            new Utilisateur { NomComplet = "Rita Réception", Email = EmailReception, MotDePasseHash = hash, Role = Role.Reception });
    }

    private static void AjouterCatalogue(CtDbContext db)
    {
        foreach (var fonctionRef in CatalogueReglementaire.Fonctions)
        {
            var fonction = new Fonction { Numero = fonctionRef.Numero, Libelle = fonctionRef.Libelle };
            db.Fonctions.Add(fonction);
            foreach (var pointRef in fonctionRef.Points)
            {
                var point = new PointControle { FonctionId = fonction.Id, Code = pointRef.Code, Libelle = pointRef.Libelle };
                point.Defaillances.AddRange(pointRef.Defaillances.Select(d => new Defaillance
                {
                    PointControleId = point.Id,
                    Code = d.Code,
                    Libelle = d.Libelle,
                    Niveau = Defaillance.NiveauDepuisCode(d.Code, point.Code)
                }));
                db.PointsControle.Add(point);
            }
        }
    }

    private static void AjouterProprietairesEtVehicules(CtDbContext db, Random aleatoire, DateTime maintenant)
    {
        var proprietaires = Enumerable.Range(1, 120).Select(_ => new Proprietaire
        {
            Nom = $"{Noms[aleatoire.Next(Noms.Length)]} {Prenoms[aleatoire.Next(Prenoms.Length)]}",
            Telephone = $"06 {aleatoire.Next(10, 99)} {aleatoire.Next(10, 99)} {aleatoire.Next(10, 99)} {aleatoire.Next(10, 99)}",
            Adresse = $"{aleatoire.Next(1, 200)} rue des Lilas, {Villes[aleatoire.Next(Villes.Length)]}"
        }).ToList();
        db.Proprietaires.AddRange(proprietaires);

        for (var i = 0; i < NombreVehicules; i++)
            db.Vehicules.Add(NouveauVehicule(aleatoire, i, $"{i:000}", proprietaires[aleatoire.Next(proprietaires.Count)].Id, maintenant));
    }

    private static Vehicule NouveauVehicule(Random aleatoire, int index, string numero, Guid proprietaireId, DateTime maintenant)
    {
        var (marque, modeles, utilitaires) = Catalogue[aleatoire.Next(Catalogue.Length)];
        var utilitaire = aleatoire.NextDouble() < 0.2;
        var aujourdhui = DateOnly.FromDateTime(maintenant);
        return new Vehicule
        {
            Immatriculation = $"{Lettres(aleatoire, 2)}-{numero}-{Lettres(aleatoire, 2)}",
            NumeroChassis = $"VF{Lettres(aleatoire, 3)}{index:000000}{aleatoire.Next(100000, 999999)}",
            Marque = marque,
            Modele = utilitaire ? utilitaires[aleatoire.Next(utilitaires.Length)] : modeles[aleatoire.Next(modeles.Length)],
            DatePremiereImmatriculation = aujourdhui.AddDays(-aleatoire.Next(200, 365 * 18)),
            TypeVehicule = utilitaire ? TypeVehicule.UtilitaireLeger : TypeVehicule.VoitureParticuliere,
            Energie = TirerEnergie(aleatoire),
            ProprietaireId = proprietaireId
        };
    }

    private static Energie TirerEnergie(Random aleatoire) => aleatoire.NextDouble() switch
    {
        < 0.45 => Energie.Essence,
        < 0.80 => Energie.Diesel,
        < 0.90 => Energie.Hybride,
        < 0.96 => Energie.Electrique,
        _ => Energie.Gpl
    };

    private static async Task AjouterHistoriqueAsync(CtDbContext db, Random aleatoire, DateTime maintenant, CancellationToken ct)
    {
        var inspecteur = await db.Utilisateurs.FirstAsync(u => u.Email == EmailInspecteur, ct);
        var catalogue = await db.PointsControle.Include(p => p.Defaillances).ToListAsync(ct);
        var vehicules = await db.Vehicules
            .Where(v => v.DatePremiereImmatriculation < DateOnly.FromDateTime(maintenant).AddYears(-4))
            .OrderBy(v => v.Immatriculation)
            .ToListAsync(ct);

        foreach (var vehicule in vehicules.OrderBy(_ => aleatoire.Next()).Take(NombreControlesHistorique))
        {
            var date = maintenant.Date.AddDays(-aleatoire.Next(3, 180)).AddHours(aleatoire.Next(8, 18));
            var controle = Controle.Ouvrir(vehicule.Id, inspecteur.Id, date, aleatoire.Next(30_000, 250_000), date);
            controle.Vehicule = vehicule;
            controle.Saisir(null, TirerSaisies(controle, catalogue, aleatoire), catalogue);
            controle.Cloturer(catalogue, date.AddMinutes(45));
            db.Controles.Add(controle);

            if (controle.Resultat != ResultatControle.Favorable && aleatoire.NextDouble() < 0.7)
            {
                var dateContreVisite = date.AddDays(aleatoire.Next(3, 40));
                if (dateContreVisite >= maintenant)
                    continue;

                var contreVisite = Controle.OuvrirContreVisite(controle, inspecteur.Id, dateContreVisite,
                    controle.Kilometrage + aleatoire.Next(20, 900), dateContreVisite);
                contreVisite.Vehicule = vehicule;
                var aReverifier = contreVisite.PointsASaisir(catalogue);
                contreVisite.Saisir(null,
                    aReverifier.Select(p => new SaisiePoint(p.Id, EtatPoint.Conforme, [], "Réparé")).ToList(),
                    catalogue);
                contreVisite.Cloturer(catalogue, dateContreVisite.AddMinutes(20));
                db.Controles.Add(contreVisite);
            }
        }
    }

    private static List<SaisiePoint> TirerSaisies(Controle controle, IReadOnlyCollection<PointControle> catalogue, Random aleatoire) =>
        controle.PointsASaisir(catalogue).Select(point =>
        {
            if (aleatoire.NextDouble() >= 0.015 || point.Defaillances.Count == 0)
                return new SaisiePoint(point.Id, EtatPoint.Conforme, [], null);

            var niveau = aleatoire.NextDouble() switch
            {
                < 0.60 => NiveauDefaillance.Mineure,
                < 0.95 => NiveauDefaillance.Majeure,
                _ => NiveauDefaillance.Critique
            };
            var candidates = point.Defaillances.Where(d => d.Niveau == niveau).ToList();
            if (candidates.Count == 0)
                candidates = point.Defaillances;
            var defaillance = candidates[aleatoire.Next(candidates.Count)];
            return new SaisiePoint(point.Id, EtatPoint.NonConforme, [defaillance.Id], null);
        }).ToList();

    public static async Task AjouterVehiculesDeChargeAsync(CtDbContext db, int totalVise, CancellationToken ct = default)
    {
        var existants = await db.Vehicules.CountAsync(ct);
        if (existants >= totalVise)
            return;

        var proprietaires = await db.Proprietaires.Select(p => p.Id).ToListAsync(ct);
        var aleatoire = new Random(existants);
        for (var i = existants; i < totalVise; i++)
        {
            db.Vehicules.Add(NouveauVehicule(aleatoire, i, $"{i:0000}", proprietaires[aleatoire.Next(proprietaires.Count)], DateTime.Now));
            if ((i + 1) % 1000 == 0)
            {
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();
            }
        }

        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
    }

    private static string Lettres(Random aleatoire, int nombre) =>
        new(Enumerable.Range(0, nombre).Select(_ => (char)('A' + aleatoire.Next(26))).ToArray());
}
