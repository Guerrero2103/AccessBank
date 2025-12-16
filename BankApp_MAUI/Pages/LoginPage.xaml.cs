using BankApp_MAUI.ViewModels;

namespace BankApp_MAUI.Pages;

public partial class LoginPage : ContentPage
{
    // Constructor met DI - zoals Agenda-master
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
