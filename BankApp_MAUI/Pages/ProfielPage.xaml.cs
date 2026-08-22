using BankApp_MAUI.ViewModels;

namespace BankApp_MAUI.Pages;

public partial class ProfielPage : ContentPage
{
    private readonly ProfielViewModel _viewModel;

    public ProfielPage(ProfielViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
