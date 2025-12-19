using BankApp_Models;

namespace BankApp_MAUI
{
    // Centrale plek voor globale variabelen 
    public static class General
    {
        // De URL van de API (poort 5000 voor HTTP support in emulator)
#if ANDROID
        public static readonly string ApiUrl = "http://10.0.2.2:5000/api/";
#else
        public static readonly string ApiUrl = "http://localhost:5000/api/";
#endif

        // De gegevens van de aangemelde gebruiker
        public static BankUser? User = null;
        public static string UserId = "";

        // De teller voor lokale ID's (altijd negatief voor ongesynchroniseerde data)
        public static long LocalIdCounter = -1;
    }
}

