namespace BankApp_MAUI.Services
{
    // Service voor instellingen bewaren
    public class LocalStorageService
    {
        // Taal instellen
        public void SaveLanguage(string languageCode)
        {
            Preferences.Set("language", languageCode);
        }

        public string GetLanguage()
        {
            return Preferences.Get("language", "nl"); // Standaard: Nederlands
        }

        // Thema instellen (licht of donker)
        public void SaveTheme(string theme)
        {
            Preferences.Set("theme", theme);
        }

        public string GetTheme()
        {
            return Preferences.Get("theme", "light");
        }

        // Tijd van laatste synchronisatie bewaren
        public void SaveLastSyncTime(DateTime syncTime)
        {
            Preferences.Set("last_sync", syncTime.ToString());
        }

        public DateTime? GetLastSyncTime()
        {
            string syncTimeStr = Preferences.Get("last_sync", string.Empty);
            if (DateTime.TryParse(syncTimeStr, out DateTime syncTime))
            {
                return syncTime;
            }
            return null;
        }

        // Automatische synchronisatie aan/uit zetten
        public void SetAutoSync(bool enabled)
        {
            Preferences.Set("auto_sync", enabled);
        }

        public bool IsAutoSyncEnabled()
        {
            return Preferences.Get("auto_sync", true); // Standaard: aan
        }
    }
}
