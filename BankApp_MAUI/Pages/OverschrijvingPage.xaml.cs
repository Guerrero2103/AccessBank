using BankApp_MAUI.ViewModels;

namespace BankApp_MAUI.Pages;

public partial class OverschrijvingPage : ContentPage
{
    private readonly OverschrijvingViewModel _viewModel;

    public OverschrijvingPage(OverschrijvingViewModel viewModel)
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
