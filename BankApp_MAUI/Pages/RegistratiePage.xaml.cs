using BankApp_MAUI.ViewModels;

namespace BankApp_MAUI.Pages;

public partial class RegistratiePage : ContentPage
{
    // Constructor met DI
    public RegistratiePage(RegistratieViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
