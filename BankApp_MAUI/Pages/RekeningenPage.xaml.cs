using BankApp_MAUI.ViewModels;

namespace BankApp_MAUI.Pages;

public partial class RekeningenPage : ContentPage
{
    private readonly RekeningenViewModel _viewModel;

    public RekeningenPage(RekeningenViewModel viewModel)
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
