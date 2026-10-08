using System.Globalization;
using System.Net.Http;
using System.Windows;
using System.Windows.Markup;
using CT.Desktop.Services;
using CT.Desktop.ViewModels;
using CT.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CT.Desktop;

public partial class App : Application
{
    private ServiceProvider _services = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("fr-FR")));
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "Erreur inattendue", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        var urlApi = Environment.GetEnvironmentVariable("CT_API_URL") ?? "http://localhost:5092/";
        var services = new ServiceCollection();
        services.AddSingleton(new HttpClient { BaseAddress = new Uri(urlApi), Timeout = TimeSpan.FromSeconds(30) });
        services.AddSingleton<Session>();
        services.AddSingleton<ApiClient>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<TableauDeBordViewModel>();
        services.AddTransient<VehiculesViewModel>();
        services.AddTransient<ProprietairesViewModel>();
        services.AddTransient<ControlesViewModel>();
        services.AddTransient<SaisieControleViewModel>();
        services.AddTransient<PointsControleViewModel>();
        services.AddTransient<UtilisateursViewModel>();
        _services = services.BuildServiceProvider();

        AfficherConnexion();
    }

    private void AfficherConnexion()
    {
        var viewModel = _services.GetRequiredService<LoginViewModel>();
        var fenetre = new LoginWindow { DataContext = viewModel };
        viewModel.ConnexionReussie += () =>
        {
            AfficherPrincipale();
            fenetre.Close();
        };
        fenetre.Show();
    }

    private void AfficherPrincipale()
    {
        var viewModel = _services.GetRequiredService<MainViewModel>();
        var fenetre = new MainWindow { DataContext = viewModel };
        viewModel.Deconnexion += () =>
        {
            AfficherConnexion();
            fenetre.Close();
        };
        fenetre.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services.Dispose();
        base.OnExit(e);
    }
}
