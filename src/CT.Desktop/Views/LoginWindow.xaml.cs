using System.Windows;
using CT.Desktop.ViewModels;

namespace CT.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow() => InitializeComponent();

    private void MotDePasseModifie(object sender, RoutedEventArgs e) =>
        ((LoginViewModel)DataContext).MotDePasse = ChampMotDePasse.Password;
}
