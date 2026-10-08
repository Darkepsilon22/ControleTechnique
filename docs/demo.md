# Démonstration en 3 minutes

Parcours complet de l'application, du point de vue des trois rôles, puis un aperçu du code et des tests.

## Préparation (5 minutes avant)

```powershell
# Base de démonstration propre (25 points, 200 véhicules, 150 contrôles)
dotnet ef database drop -f -p src/CT.Infrastructure -s src/CT.Api
dotnet run --project src/CT.Api

# Second terminal
dotnet run --project src/CT.Desktop
```

À garder ouverts :

- le navigateur sur <http://localhost:5092/scalar/v1> ;
- l'éditeur sur `src/CT.Domain/Entities/Controle.cs` et `src/CT.Domain/Rules/CalculateurResultat.cs` ;
- un terminal à la racine du dépôt, pour `dotnet test`.

Tous les comptes utilisent le mot de passe `Demo123!`.

## Déroulé

| Temps | Rôle | Action | À dire |
| --- | --- | --- | --- |
| 0:00 – 0:20 | — | Écran de connexion | Une API ASP.NET Core avec SQL Server, et une application WPF pour trois rôles. Tout est en C# .NET 10. |
| 0:20 – 0:50 | Réception (`reception@ct.local`) | Véhicules → **Nouveau** → immatriculation `DE-123-MO`, châssis `VF1DEMO0000000001`, propriétaire recherché par son nom → **Enregistrer**. Puis **Nouveau** à nouveau, même immatriculation et un autre châssis (`VF1DEMO0000000002`) → **Enregistrer**. | La validation se fait côté serveur. L'immatriculation est unique : l'API répond 409 et l'écran affiche le message. |
| 0:50 – 1:50 | Inspecteur (`inspecteur@ct.local`) | Véhicules → rechercher `DE-123` et sélectionner le véhicule → kilométrage `85000` → **Ouvrir un contrôle** → **Tout conforme** → passer « Essuie-glaces » en **Non conforme** avec un commentaire → **Clôturer le contrôle** → **Oui** à la confirmation → **Oui** pour ouvrir le PV. | La checklist est groupée par catégorie. Un point mineur non conforme donne un résultat favorable avec une observation, et la date de validité est à 12 mois. Après la clôture, plus aucune modification n'est possible. Le PV est généré en PDF par l'API. |
| 1:50 – 2:20 | Administrateur (`admin@ct.local`) | Tableau de bord → Points de contrôle → Utilisateurs | Les statistiques sont calculées en SQL par Entity Framework. Un point désactivé n'est jamais supprimé, pour garder l'historique des anciens contrôles. |
| 2:20 – 2:45 | — | Éditeur : `Controle.Cloturer` et `CalculateurResultat.Calculer`, puis `dotnet test` dans le terminal | Les règles métier sont dans le domaine, pas dans les contrôleurs ni dans l'interface. 46 tests passent : des tests unitaires des règles et des tests d'intégration de l'API sur SQLite en mémoire. |
| 2:45 – 3:00 | — | Navigateur : Scalar | L'API est documentée avec OpenAPI et sécurisée par JWT. Avec 10 000 véhicules, chaque requête répond en moins de 40 ms. La CI GitHub Actions compile et teste chaque pull request. |

## En cas de problème

- L'API ne démarre pas : vérifier que LocalDB tourne (`sqllocaldb start MSSQLLocalDB`) et que la clé JWT est définie (`dotnet user-secrets list` dans `src/CT.Api`).
- L'application affiche « Impossible de joindre l'API » : l'API n'est pas lancée, ou elle écoute sur une autre adresse (variable `CT_API_URL`).
- Si la démo en direct échoue, montrer les captures du README.
