using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CT.Desktop.Services;

namespace CT.Desktop.ViewModels;

public record ElementMenu(string Titre, Func<Task> Ouvrir);

public partial class MainViewModel : ObservableObject
{
    private readonly Session _session;

    [ObservableProperty] private ViewModelBase? _pageCourante;
    [ObservableProperty] private ElementMenu? _menuSelectionne;

    public ObservableCollection<ElementMenu> Menu { get; } = [];
    public string NomUtilisateur => _session.Utilisateur?.NomComplet ?? "";
    public string Role => _session.Utilisateur?.Role.ToString() ?? "";

    public event Action? Deconnexion;

    public MainViewModel(Session session, NavigationService navigation)
    {
        _session = session;
        navigation.PageChangee += page => PageCourante = page;

        if (session.EstAdministrateur)
            Menu.Add(new("Tableau de bord", () => navigation.NaviguerAsync<TableauDeBordViewModel>()));
        Menu.Add(new("Véhicules", () => navigation.NaviguerAsync<VehiculesViewModel>()));
        if (session.PeutGererVehicules)
            Menu.Add(new("Propriétaires", () => navigation.NaviguerAsync<ProprietairesViewModel>()));
        Menu.Add(new(session.EstInspecteur ? "Mes contrôles" : "Contrôles", () => navigation.NaviguerAsync<ControlesViewModel>()));
        if (session.EstAdministrateur)
        {
            Menu.Add(new("Points de contrôle", () => navigation.NaviguerAsync<PointsControleViewModel>()));
            Menu.Add(new("Utilisateurs", () => navigation.NaviguerAsync<UtilisateursViewModel>()));
        }

        MenuSelectionne = Menu[0];
    }

    partial void OnMenuSelectionneChanged(ElementMenu? value) => value?.Ouvrir();

    [RelayCommand]
    private void Deconnecter()
    {
        _session.Fermer();
        Deconnexion?.Invoke();
    }
}
