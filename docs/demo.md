# Démonstration en 3 minutes

Parcours complet de l'application, du point de vue des trois rôles, puis un aperçu du code et des tests.

## Préparation (5 minutes avant)

```powershell
# Base de démonstration propre (catalogue réglementaire, 200 véhicules, environ 150 contrôles)
dotnet ef database drop -f -p src/CT.Infrastructure -s src/CT.Api
dotnet run --project src/CT.Api

# Second terminal
dotnet run --project src/CT.Desktop
```

À garder ouverts :

- le navigateur sur <http://localhost:5092/scalar/v1> ;
- l'éditeur sur `src/CT.Domain/Rules/Reglementation.cs`, `src/CT.Domain/Rules/RegleContreVisite.cs` et `src/CT.Domain/Entities/Controle.cs` ;
- un terminal à la racine du dépôt, pour `dotnet test`.

Tous les comptes utilisent le mot de passe `Demo123!`.

## Déroulé

| Temps | Rôle | Action | À dire |
| --- | --- | --- | --- |
| 0:00 – 0:20 | — | Écran de connexion | Une API ASP.NET Core avec SQL Server, et une application WPF pour trois rôles. Tout est en C# .NET 10. |
| 0:20 – 0:50 | Réception (`reception@ct.local`) | Véhicules → **Nouveau** → immatriculation `DE-123-MO`, châssis `VF1DEMO0000000001`, propriétaire recherché par son nom → **Enregistrer**. Puis **Nouveau** à nouveau, même immatriculation et un autre châssis (`VF1DEMO0000000002`) → **Enregistrer**. | La validation se fait côté serveur. L'immatriculation est unique : l'API répond 409 et l'écran affiche le message. |
| 0:50 – 1:50 | Inspecteur (`inspecteur@ct.local`) | Véhicules → rechercher `DE-123` et sélectionner le véhicule → kilométrage `85000` → **Ouvrir un contrôle** → **Tout conforme** → sur le point 1.1.13, choisir **Défaillance(s)** et cocher `1.1.13.a.2` → **Clôturer le contrôle** → **Oui** à la confirmation → **Oui** pour ouvrir le PV. Revenir aux contrôles, sélectionner ce contrôle et ouvrir la contre-visite. | La checklist suit les fonctions de l'annexe I de l'arrêté du 18 juin 1991, avec les codes officiels des défaillances. Une défaillance majeure donne le résultat S : contre-visite sous 2 mois. La contre-visite revérifie toute la fonction freinage, l'identification et le compteur, comme l'impose l'annexe I. |
| 1:50 – 2:20 | Administrateur (`admin@ct.local`) | Tableau de bord → Points de contrôle → Utilisateurs | Les statistiques distinguent les résultats A, S et R et listent les défaillances les plus fréquentes. Le catalogue est administrable : le niveau d'une défaillance se déduit de son code. |
| 2:20 – 2:45 | — | Éditeur : `Reglementation.Resultat`, `RegleContreVisite.PointsAReverifier` et `Controle.Cloturer`, puis `dotnet test` dans le terminal | Les règles métier sont dans le domaine, pas dans les contrôleurs ni dans l'interface. 75 tests passent : des tests unitaires des règles et des tests d'intégration de l'API sur SQLite en mémoire. |
| 2:45 – 3:00 | — | Navigateur : Scalar | L'API est documentée avec OpenAPI et sécurisée par JWT. Avec 10 000 véhicules, chaque requête répond en moins de 40 ms. La CI GitHub Actions compile et teste chaque pull request. |

## En cas de problème

- L'API ne démarre pas : vérifier que LocalDB tourne (`sqllocaldb start MSSQLLocalDB`) et que la clé JWT est définie (`dotnet user-secrets list` dans `src/CT.Api`).
- L'application affiche « Impossible de joindre l'API » : l'API n'est pas lancée, ou elle écoute sur une autre adresse (variable `CT_API_URL`).
- Si la démo en direct échoue, montrer les captures du README.
