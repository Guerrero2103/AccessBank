using BankApp_Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_BusinessLogic
{
    public class TransactieService : ITransactieService
    {
        private readonly AppDbContext _context;

        public TransactieService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Transactie> GetTransactieByIdAsync(int transactieId)
        {
            return await _context.Transacties
                .Where(t => t.Deleted == DateTime.MaxValue)
                .Include(t => t.Gebruiker)
                .FirstOrDefaultAsync(t => t.Id == transactieId);
        }

        public async Task<List<Transactie>> GetTransactiesByRekeningIdAsync(int rekeningId, int aantal = 50)
        {
            var rekening = await _context.Rekeningen
                .FirstOrDefaultAsync(r => r.Id == rekeningId && r.Deleted == DateTime.MaxValue);
            if (rekening == null)
                return new List<Transactie>();

            return await _context.Transacties
                .Where(t => t.Deleted == DateTime.MaxValue)
                .Where(t => t.VanIban == rekening.Iban || t.NaarIban == rekening.Iban)
                .OrderByDescending(t => t.Datum)
                .Take(aantal)
                .ToListAsync();
        }

        public async Task<List<Transactie>> GetTransactiesByGebruikerIdAsync(string gebruikerId, int aantal = 50)
        {
            // Haal alle rekeningnummers van gebruiker op
            var gebruikerIbans = await (from rekening in _context.Rekeningen
                                        where rekening.GebruikerId == gebruikerId && rekening.Deleted == DateTime.MaxValue
                                        select rekening.Iban)
                .ToListAsync();

            // Haal transacties op die bij deze rekeningen horen
            return await _context.Transacties
                .Where(t => t.Deleted == DateTime.MaxValue)
                .Where(t => gebruikerIbans.Contains(t.VanIban) || gebruikerIbans.Contains(t.NaarIban))
                .OrderByDescending(t => t.Datum)
                .Take(aantal)
                .ToListAsync();
        }

        public async Task<(bool Succes, string Bericht, Transactie Transactie)> MaakOverschrijvingAsync(
            string vanIban,
            string naarIban,
            decimal bedrag,
            string omschrijving,
            string gebruikerId)
        {
            if (bedrag <= 0)
                return (false, "Bedrag moet groter zijn dan 0", null);

            var vanRekening = await _context.Rekeningen
                .FirstOrDefaultAsync(r => r.Iban == vanIban && r.Deleted == DateTime.MaxValue);

            if (vanRekening == null)
                return (false, "Bronrekening niet gevonden", null);

            if (vanRekening.GebruikerId != gebruikerId)
                return (false, "U bent niet gemachtigd voor deze rekening", null);

            if (vanRekening.Saldo < bedrag)
                return (false, "Onvoldoende saldo", null);

            var naarRekening = await _context.Rekeningen
                .FirstOrDefaultAsync(r => r.Iban == naarIban && r.Deleted == DateTime.MaxValue);

            if (naarRekening == null)
                return (false, "Doelrekening niet gevonden", null);

            if (vanIban == naarIban)
                return (false, "Kan niet naar dezelfde rekening overschrijven", null);

            // Controleer op verdachte transacties
            var fraudCheck = await ControleerFraudPatternAsync(gebruikerId, bedrag);
            if (fraudCheck.IsVerdacht)
            {
                await BevriesKaartenVanGebruikerAsync(gebruikerId);
                return (false, $"Transactie geweigerd: Verdacht patroon gedetecteerd. {fraudCheck.Reden}. Uw kaart is tijdelijk bevroren voor uw veiligheid.", null);
            }

            // Bij bedrag van 500 euro of meer moet medewerker goedkeuren
            const decimal WACHTEND_DREMPEL = 500.00m;
            bool moetWachten = bedrag >= WACHTEND_DREMPEL;

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Bij bedrag onder 500 euro: direct geld overmaken
                if (!moetWachten)
            {
                vanRekening.Saldo -= bedrag;
                naarRekening.Saldo += bedrag;
                }
                // Bij 500 euro of meer: wachten op goedkeuring medewerker

                var transactie = new Transactie
                {
                    VanIban = vanIban,
                    NaarIban = naarIban,
                    NaamOntvanger = naarRekening.Gebruiker?.Email ?? "Onbekend",
                    Bedrag = bedrag,
                    Omschrijving = omschrijving,
                    Datum = DateTime.Now,
                    GebruikerId = gebruikerId,
                    Status = moetWachten ? TransactieStatus.Wachtend : TransactieStatus.Voltooid,
                    Deleted = DateTime.MaxValue
                };

                _context.Transacties.Add(transactie);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                string bericht = moetWachten 
                    ? "Transactie is in behandeling. Een medewerker zal u bellen voor bevestiging." 
                    : "Transactie succesvol uitgevoerd";

                return (true, bericht, transactie);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return (false, $"Transactie mislukt: {ex.Message}", null);
            }
        }

        // Controleer of gebruiker verdachte transacties doet
        // Bijvoorbeeld: veel kleine transacties en dan ineens een groot bedrag
        private async Task<(bool IsVerdacht, string Reden)> ControleerFraudPatternAsync(string gebruikerId, decimal huidigBedrag)
        {
            // Haal alle rekeningnummers van gebruiker op
            var gebruikerIbans = await (from rekening in _context.Rekeningen
                                       where rekening.GebruikerId == gebruikerId && rekening.Deleted == DateTime.MaxValue
                                       select rekening.Iban)
                                      .ToListAsync();

            if (!gebruikerIbans.Any())
                return (false, string.Empty);

            // Kijk naar transacties van de laatste 30 minuten
            var tijdVenster = DateTime.Now.AddMinutes(-30);

            // Haal recente transacties op
            var recenteTransacties = await (from transactie in _context.Transacties
                                           where transactie.Deleted == DateTime.MaxValue &&
                                                 gebruikerIbans.Contains(transactie.VanIban) &&
                                                 transactie.Datum >= tijdVenster
                                           orderby transactie.Datum descending
                                           select transactie)
                                          .ToListAsync();

            int aantalRecenteTransacties = recenteTransacties.Count;

            // Instellingen voor controle
            const int MIN_AANTAL_TRANSACTIES = 4; // Minimaal 4 transacties in korte tijd
            const decimal GROOT_BEDRAG_DREMPEL = 1000.00m; // Bedrag groter dan 1000 euro is verdacht

            // Controleer patroon: veel transacties in korte tijd en dan een groot bedrag
            if (aantalRecenteTransacties >= MIN_AANTAL_TRANSACTIES && huidigBedrag >= GROOT_BEDRAG_DREMPEL)
            {
                decimal totaalBedrag = recenteTransacties.Sum(t => t.Bedrag) + huidigBedrag;
                return (true, $"Verdacht patroon: {aantalRecenteTransacties + 1} transacties in 30 minuten met totaalbedrag van {totaalBedrag:C}");
            }

            return (false, string.Empty);
        }

        // Blokkeer alle kaarten van gebruiker
        private async Task BevriesKaartenVanGebruikerAsync(string gebruikerId)
        {
            // Haal alle actieve kaarten van gebruiker op
            var kaarten = await (from kaart in _context.Kaarten
                                where kaart.GebruikerId == gebruikerId &&
                                      kaart.Deleted == DateTime.MaxValue &&
                                      kaart.Status == KaartStatus.Actief
                                select kaart)
                               .ToListAsync();

            foreach (var kaart in kaarten)
            {
                kaart.Status = KaartStatus.Bevroren;
            }

            if (kaarten.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        // Haal alle overschrijvingen op die wachten op goedkeuring
        public async Task<List<Transactie>> GetWachtendeOverschrijvingenAsync()
        {
            return await _context.Transacties
                .Where(t => t.Deleted == DateTime.MaxValue && t.Status == TransactieStatus.Wachtend)
                .Include(t => t.Gebruiker)
                .ThenInclude(g => g.Adres)
                .OrderByDescending(t => t.Datum)
                .ToListAsync();
        }

        // Medewerker keurt overschrijving goed
        public async Task<(bool Succes, string Bericht)> BevestigOverschrijvingAsync(int transactieId, string medewerkerId)
        {
            var transactie = await _context.Transacties
                .Include(t => t.Gebruiker)
                .FirstOrDefaultAsync(t => t.Id == transactieId && t.Deleted == DateTime.MaxValue);

            if (transactie == null)
                return (false, "Transactie niet gevonden");

            if (transactie.Status != TransactieStatus.Wachtend)
                return (false, "Transactie is niet in wachtende status");

            var vanRekening = await _context.Rekeningen
                .FirstOrDefaultAsync(r => r.Iban == transactie.VanIban && r.Deleted == DateTime.MaxValue);

            var naarRekening = await _context.Rekeningen
                .FirstOrDefaultAsync(r => r.Iban == transactie.NaarIban && r.Deleted == DateTime.MaxValue);

            if (vanRekening == null || naarRekening == null)
                return (false, "Rekening niet gevonden");

            if (vanRekening.Saldo < transactie.Bedrag)
                return (false, "Onvoldoende saldo op rekening");

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Haal geld af van rekening en zet op andere rekening
                vanRekening.Saldo -= transactie.Bedrag;
                naarRekening.Saldo += transactie.Bedrag;

                // Zet status op voltooid
                transactie.Status = TransactieStatus.Voltooid;
                transactie.BevestigdOp = DateTime.Now;
                transactie.BevestigdDoor = medewerkerId;

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return (true, "Overschrijving bevestigd en uitgevoerd");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return (false, $"Fout bij bevestigen: {ex.Message}");
            }
        }

        // Medewerker wijst overschrijving af
        public async Task<(bool Succes, string Bericht)> AfwijsOverschrijvingAsync(int transactieId, string medewerkerId, string reden)
        {
            var transactie = await _context.Transacties
                .FirstOrDefaultAsync(t => t.Id == transactieId && t.Deleted == DateTime.MaxValue);

            if (transactie == null)
                return (false, "Transactie niet gevonden");

            if (transactie.Status != TransactieStatus.Wachtend)
                return (false, "Transactie is niet in wachtende status");

            try
            {
                transactie.Status = TransactieStatus.Afgewezen;
                transactie.AfwijzingsReden = reden;
                transactie.BevestigdOp = DateTime.Now;
                transactie.BevestigdDoor = medewerkerId;

                await _context.SaveChangesAsync();

                return (true, "Overschrijving afgewezen");
            }
            catch (Exception ex)
            {
                return (false, $"Fout bij afwijzen: {ex.Message}");
            }
        }
    }
}