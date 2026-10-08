using CT.Domain.Entities;
using CT.Domain.Enums;
using CT.Domain.Rules;
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

    private static readonly (string Categorie, (string Libelle, Gravite Gravite)[] Points)[] Referentiel =
    [
        ("Freinage",
        [
            ("Efficacité du frein de service", Gravite.Critique),
            ("Efficacité du frein de stationnement", Gravite.Majeur),
            ("Étanchéité du circuit de freinage", Gravite.Critique),
            ("Usure des disques et tambours", Gravite.Majeur),
            ("Usure des plaquettes", Gravite.Mineur)
        ]),
        ("Direction",
        [
            ("Jeu dans la direction", Gravite.Critique),
            ("État des rotules", Gravite.Majeur),
            ("Fonctionnement de l'assistance", Gravite.Mineur)
        ]),
        ("Éclairage et signalisation",
        [
            ("Feux de croisement", Gravite.Majeur),
            ("Feux stop", Gravite.Majeur),
            ("Feux de route", Gravite.Mineur),
            ("Clignotants", Gravite.Mineur),
            ("Réglage des projecteurs", Gravite.Mineur)
        ]),
        ("Pneumatiques et liaisons au sol",
        [
            ("Fixation des roues", Gravite.Critique),
            ("Profondeur des sculptures", Gravite.Majeur),
            ("État des flancs", Gravite.Majeur),
            ("Amortisseurs", Gravite.Mineur)
        ]),
        ("Visibilité",
        [
            ("État du pare-brise", Gravite.Mineur),
            ("Essuie-glaces", Gravite.Mineur),
            ("Rétroviseurs", Gravite.Mineur)
        ]),
        ("Émissions",
        [
            ("Opacité des fumées", Gravite.Majeur),
            ("Fuites de liquides", Gravite.Majeur),
            ("État de l'échappement", Gravite.Mineur)
        ]),
        ("Équipements",
        [
            ("Ceintures de sécurité", Gravite.Critique),
            ("Avertisseur sonore", Gravite.Mineur)
        ])
    ];

    private static readonly (string Marque, string[] Modeles)[] Catalogue =
    [
        ("Renault", ["Clio", "Mégane", "Kangoo", "Master"]),
        ("Peugeot", ["208", "308", "3008", "Partner"]),
        ("Citroën", ["C3", "C4", "Berlingo", "Jumper"]),
        ("Toyota", ["Yaris", "Corolla", "Hilux", "Land Cruiser"]),
        ("Volkswagen", ["Polo", "Golf", "Transporter"]),
        ("Hyundai", ["i10", "i20", "Tucson"]),
        ("Ford", ["Fiesta", "Ranger", "Transit"])
    ];

    private static readonly string[] Prenoms =
        ["Jean", "Marie", "Paul", "Sophie", "Luc", "Claire", "Hugo", "Emma", "Louis", "Alice", "Nicolas", "Julie", "Thomas", "Laura", "Marc"];

    private static readonly string[] Noms =
        ["Martin", "Bernard", "Dubois", "Durand", "Lefebvre", "Moreau", "Laurent", "Simon", "Michel", "Garcia", "Roux", "Fournier", "Girard", "Bonnet", "Mercier"];

    private static readonly string[] Villes = ["Lyon", "Nantes", "Lille", "Rennes", "Dijon", "Tours", "Angers", "Nancy"];

    public static async Task SeedAsync(CtDbContext db, IHachageMotDePasse hachage, TimeProvider horloge, CancellationToken ct = default)
    {
        if (await db.Utilisateurs.AnyAsync(ct))
            return;

        var maintenant = horloge.GetLocalNow().DateTime;
        var aleatoire = new Random(42);

        var hashDemo = hachage.Hacher(MotDePasseDemo);
        var admin = new Utilisateur { NomComplet = "Alice Admin", Email = EmailAdmin, MotDePasseHash = hashDemo, Role = Role.Administrateur };
        var inspecteur = new Utilisateur { NomComplet = "Ivan Inspecteur", Email = EmailInspecteur, MotDePasseHash = hashDemo, Role = Role.Inspecteur };
        var reception = new Utilisateur { NomComplet = "Rita Réception", Email = EmailReception, MotDePasseHash = hashDemo, Role = Role.Reception };
        db.Utilisateurs.AddRange(admin, inspecteur, reception);

        var points = new List<PointControle>();
        foreach (var (libelleCategorie, pointsCategorie) in Referentiel)
        {
            var categorie = new CategoriePoint { Libelle = libelleCategorie };
            db.CategoriesPoints.Add(categorie);
            foreach (var (libelle, gravite) in pointsCategorie)
            {
                var point = new PointControle { Categorie = categorie, CategorieId = categorie.Id, Libelle = libelle, Gravite = gravite };
                points.Add(point);
                db.PointsControle.Add(point);
            }
        }

        var proprietaires = Enumerable.Range(1, 120).Select(i => new Proprietaire
        {
            Nom = $"{Noms[aleatoire.Next(Noms.Length)]} {Prenoms[aleatoire.Next(Prenoms.Length)]}",
            Telephone = $"06 {aleatoire.Next(10, 99)} {aleatoire.Next(10, 99)} {aleatoire.Next(10, 99)} {aleatoire.Next(10, 99)}",
            Adresse = $"{aleatoire.Next(1, 200)} rue des Lilas, {Villes[aleatoire.Next(Villes.Length)]}"
        }).ToList();
        db.Proprietaires.AddRange(proprietaires);

        var vehicules = new List<Vehicule>();
        for (var i = 0; i < NombreVehicules; i++)
        {
            var (marque, modeles) = Catalogue[aleatoire.Next(Catalogue.Length)];
            vehicules.Add(new Vehicule
            {
                Immatriculation = $"{Lettres(aleatoire, 2)}-{i:000}-{Lettres(aleatoire, 2)}",
                NumeroChassis = $"VF{Lettres(aleatoire, 3)}{i:000000}{aleatoire.Next(100000, 999999)}",
                Marque = marque,
                Modele = modeles[aleatoire.Next(modeles.Length)],
                Annee = aleatoire.Next(2005, maintenant.Year + 1),
                TypeVehicule = (TypeVehicule)aleatoire.Next(0, 3),
                Energie = (Energie)aleatoire.Next(0, 5),
                Proprietaire = proprietaires[aleatoire.Next(proprietaires.Count)]
            });
        }
        db.Vehicules.AddRange(vehicules);

        var pointsActifs = points.Select(p => p.Id).ToList();
        for (var i = 0; i < NombreControlesHistorique; i++)
        {
            var vehicule = vehicules[aleatoire.Next(vehicules.Count)];
            var date = maintenant.Date.AddDays(-aleatoire.Next(1, 180)).AddHours(aleatoire.Next(8, 18));
            var controle = Controle.Ouvrir(vehicule.Id, inspecteur.Id, date, aleatoire.Next(5_000, 250_000), date);
            controle.Saisir(null, points.Select(p => new SaisiePoint(
                p.Id,
                aleatoire.NextDouble() < 0.015 ? EtatPoint.NonConforme : EtatPoint.Conforme,
                null)).ToList());
            foreach (var resultat in controle.Resultats)
                resultat.PointControle = points.First(p => p.Id == resultat.PointControleId);
            controle.Cloturer(pointsActifs, CalculateurResultat.DureeValiditeParDefautMois, date.AddMinutes(45));
            db.Controles.Add(controle);
        }

        await db.SaveChangesAsync(ct);
    }

    public static async Task AjouterVehiculesDeChargeAsync(CtDbContext db, int totalVise, CancellationToken ct = default)
    {
        var existants = await db.Vehicules.CountAsync(ct);
        if (existants >= totalVise)
            return;

        var proprietaires = await db.Proprietaires.Select(p => p.Id).ToListAsync(ct);
        var aleatoire = new Random(existants);
        for (var i = existants; i < totalVise; i++)
        {
            var (marque, modeles) = Catalogue[aleatoire.Next(Catalogue.Length)];
            db.Vehicules.Add(new Vehicule
            {
                Immatriculation = $"{Lettres(aleatoire, 2)}-{i:0000}-{Lettres(aleatoire, 2)}",
                NumeroChassis = $"VF{Lettres(aleatoire, 3)}{i:000000}{aleatoire.Next(100000, 999999)}",
                Marque = marque,
                Modele = modeles[aleatoire.Next(modeles.Length)],
                Annee = aleatoire.Next(2005, DateTime.Today.Year + 1),
                TypeVehicule = (TypeVehicule)aleatoire.Next(0, 3),
                Energie = (Energie)aleatoire.Next(0, 5),
                ProprietaireId = proprietaires[aleatoire.Next(proprietaires.Count)]
            });

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
