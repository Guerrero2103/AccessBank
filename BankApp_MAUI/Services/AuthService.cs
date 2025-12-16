namespace BankApp_MAUI.Services
{
    // Service voor inloggegevens beheren
    public class AuthService
    {
        private const string TOKEN_KEY = "auth_token";
        private const string USER_ID_KEY = "user_id";
        private const string USER_EMAIL_KEY = "user_email";

        // Bewaar inlogtoken
        public void SaveToken(string token)
        {
            Preferences.Set(TOKEN_KEY, token);
        }

        // Haal inlogtoken op
        public string GetToken()
        {
            return Preferences.Get(TOKEN_KEY, string.Empty);
        }

        // Controleer of gebruiker is ingelogd
        public bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(GetToken());
        }

        // Bewaar gebruikersgegevens
        public void SaveUserInfo(string userId, string email)
        {
            Preferences.Set(USER_ID_KEY, userId);
            Preferences.Set(USER_EMAIL_KEY, email);
        }

        // Haal gebruikersgegevens op
        public (string UserId, string Email) GetUserInfo()
        {
            string userId = Preferences.Get(USER_ID_KEY, string.Empty);
            string email = Preferences.Get(USER_EMAIL_KEY, string.Empty);
            return (userId, email);
        }

        // Uitloggen - verwijder alle gegevens
        public void Logout()
        {
            Preferences.Remove(TOKEN_KEY);
            Preferences.Remove(USER_ID_KEY);
            Preferences.Remove(USER_EMAIL_KEY);
        }
    }
}
