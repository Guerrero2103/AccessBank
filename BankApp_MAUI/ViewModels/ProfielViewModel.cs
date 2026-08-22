using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BankApp_MAUI.ViewModels
{
    public partial class ProfielViewModel : BaseViewModel
    {
        private readonly Synchronizer _synchronizer;

        [ObservableProperty]
        private string voornaam = string.Empty;

        [ObservableProperty]
        private string achternaam = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string telefoonnummer = string.Empty;

        [ObservableProperty]
        private string straat = string.Empty;

        [ObservableProperty]
        private string huisnummer = string.Empty;

        [ObservableProperty]
        private string bus = string.Empty;

        [ObservableProperty]
        private string postcode = string.Empty;

        [ObservableProperty]
        private string gemeente = string.Empty;

        [ObservableProperty]
        private string land = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        public ProfielViewModel(Synchronizer synchronizer)
        {
            _synchronizer = synchronizer;
            Title = "Mijn profiel";
        }

        public async Task InitializeAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var profiel = await _synchronizer.HaalProfielOpAsync();
                if (profiel != null)
                {
                    Voornaam = profiel.voornaam;
                    Achternaam = profiel.achternaam;
                    Email = profiel.email;
                    Telefoonnummer = profiel.telefoonnummer ?? string.Empty;
                    Straat = profiel.straat ?? string.Empty;
                    Huisnummer = profiel.huisnummer ?? string.Empty;
                    Bus = profiel.bus ?? string.Empty;
                    Postcode = profiel.postcode ?? string.Empty;
                    Gemeente = profiel.gemeente ?? string.Empty;
                    Land = profiel.land ?? string.Empty;
                }
                else
                {
                    ErrorMessage = "Kon profiel niet ophalen. Controleer je internetverbinding.";
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task OpslaanAsync()
        {
            if (IsBusy) return;

            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Voornaam) || string.IsNullOrWhiteSpace(Achternaam))
            {
                ErrorMessage = "Voornaam en achternaam zijn verplicht.";
                return;
            }

            IsBusy = true;

            try
            {
                var (succes, foutBoodschap) = await _synchronizer.WerkProfielBijAsync(
                    Voornaam, Achternaam, Telefoonnummer,
                    Straat, Huisnummer, Bus, Postcode, Gemeente, Land);

                if (succes)
                {
                    SuccessMessage = "Profiel succesvol bijgewerkt!";
                }
                else
                {
                    ErrorMessage = foutBoodschap ?? "Bijwerken mislukt. Probeer opnieuw.";
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
