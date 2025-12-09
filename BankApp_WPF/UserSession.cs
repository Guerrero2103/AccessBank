using BankApp_Models;

namespace BankApp_WPF
{
    public static class UserSession
    {
        public static BankUser? IngelogdeGebruiker { get; set; }

        public static void LogUit()
        {
            IngelogdeGebruiker = null;
        }

        public static bool IsIngelogd => IngelogdeGebruiker != null;
    }
}

