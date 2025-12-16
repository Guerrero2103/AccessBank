using BankApp_MAUI.ViewModels;

namespace BankApp_MAUI.Pages;

public partial class TransactiesPage : ContentPage
{
    private readonly TransactiesViewModel _viewModel;

    public TransactiesPage(TransactiesViewModel viewModel)
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
