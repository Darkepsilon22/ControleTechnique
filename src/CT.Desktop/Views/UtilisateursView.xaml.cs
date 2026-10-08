using System.Windows;
using System.Windows.Controls;
using CT.Desktop.ViewModels;

namespace CT.Desktop.Views;

public partial class UtilisateursView : UserControl
{
    public UtilisateursView() => InitializeComponent();

    private void ContexteModifie(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is UtilisateursViewModel ancien)
            ancien.MotDePasseEfface -= EffacerMotDePasse;
        if (e.NewValue is UtilisateursViewModel nouveau)
            nouveau.MotDePasseEfface += EffacerMotDePasse;
    }

    private void EffacerMotDePasse() => ChampMotDePasse.Clear();

    private void MotDePasseModifie(object sender, RoutedEventArgs e)
    {
        if (DataContext is UtilisateursViewModel vm)
            vm.MotDePasse = ChampMotDePasse.Password;
    }
}
