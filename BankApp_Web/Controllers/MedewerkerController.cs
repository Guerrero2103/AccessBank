using BankApp_Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_Web.Controllers
{
    [Authorize(Roles = "Medewerker,Admin")]
    public class MedewerkerController : Controller
    {
        private readonly AppDbContext _context;

        public MedewerkerController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Medewerker - Overzicht wachtende transacties
        public async Task<IActionResult> Index(string searchString, string sortOrder)
        {
            var wachtendeTransacties = _context.Transacties
                .Where(t => t.Deleted == DateTime.MaxValue && t.Status == TransactieStatus.Wachtend)
                .Include(t => t.Gebruiker)
                .ThenInclude(g => g.Adres)
                .AsQueryable();

            // Zoek functionaliteit
            ViewData["CurrentFilter"] = searchString;
            if (!string.IsNullOrEmpty(searchString))
            {
                wachtendeTransacties = wachtendeTransacties.Where(t => 
                    t.VanIban.Contains(searchString) ||
                    t.NaarIban.Contains(searchString) ||
                    t.Gebruiker.Email.Contains(searchString) ||
                    t.Gebruiker.Voornaam.Contains(searchString) ||
                    t.Gebruiker.Achternaam.Contains(searchString));
            }

            // Sortering
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DatumSortParm"] = string.IsNullOrEmpty(sortOrder) ? "datum_desc" : "";
            ViewData["VanIbanSortParm"] = sortOrder == "VanIban" ? "vaniban_desc" : "VanIban";
            ViewData["NaarIbanSortParm"] = sortOrder == "NaarIban" ? "naariban_desc" : "NaarIban";
            ViewData["BedragSortParm"] = sortOrder == "Bedrag" ? "bedrag_desc" : "Bedrag";
            ViewData["GebruikerSortParm"] = sortOrder == "Gebruiker" ? "gebruiker_desc" : "Gebruiker";

            switch (sortOrder)
            {
                case "datum_desc":
                    wachtendeTransacties = wachtendeTransacties.OrderByDescending(t => t.Datum);
                    break;
                case "VanIban":
                    wachtendeTransacties = wachtendeTransacties.OrderBy(t => t.VanIban);
                    break;
                case "vaniban_desc":
                    wachtendeTransacties = wachtendeTransacties.OrderByDescending(t => t.VanIban);
                    break;
                case "NaarIban":
                    wachtendeTransacties = wachtendeTransacties.OrderBy(t => t.NaarIban);
                    break;
                case "naariban_desc":
                    wachtendeTransacties = wachtendeTransacties.OrderByDescending(t => t.NaarIban);
                    break;
                case "Bedrag":
                    wachtendeTransacties = wachtendeTransacties.OrderBy(t => t.Bedrag);
                    break;
                case "bedrag_desc":
                    wachtendeTransacties = wachtendeTransacties.OrderByDescending(t => t.Bedrag);
                    break;
                case "Gebruiker":
                    wachtendeTransacties = wachtendeTransacties.OrderBy(t => t.Gebruiker.Email);
                    break;
                case "gebruiker_desc":
                    wachtendeTransacties = wachtendeTransacties.OrderByDescending(t => t.Gebruiker.Email);
                    break;
                default:
                    wachtendeTransacties = wachtendeTransacties.OrderByDescending(t => t.Datum);
                    break;
            }

            return View(await wachtendeTransacties.ToListAsync());
        }

        // GET: Medewerker/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transactie = await _context.Transacties
                .Include(t => t.Gebruiker)
                .ThenInclude(g => g.Adres)
                .FirstOrDefaultAsync(m => m.Id == id && m.Deleted == DateTime.MaxValue);

            if (transactie == null)
            {
                return NotFound();
            }

            return View(transactie);
        }

        // POST: Medewerker/Bevestig/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bevestig(int id)
        {
            var transactie = await _context.Transacties
                .FirstOrDefaultAsync(t => t.Id == id && t.Deleted == DateTime.MaxValue);

            if (transactie == null || transactie.Status != TransactieStatus.Wachtend)
            {
                return NotFound();
            }

            string medewerkerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;

            var vanRekening = await _context.Rekeningen
                .FirstOrDefaultAsync(r => r.Iban == transactie.VanIban && r.Deleted == DateTime.MaxValue);
            var naarRekening = await _context.Rekeningen
                .FirstOrDefaultAsync(r => r.Iban == transactie.NaarIban && r.Deleted == DateTime.MaxValue);

            if (vanRekening == null || naarRekening == null)
            {
                return NotFound();
            }

            if (vanRekening.Saldo < transactie.Bedrag)
            {
                TempData["Error"] = "Onvoldoende saldo op rekening";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                vanRekening.Saldo -= transactie.Bedrag;
                naarRekening.Saldo += transactie.Bedrag;

                transactie.Status = TransactieStatus.Voltooid;
                transactie.BevestigdOp = DateTime.Now;
                transactie.BevestigdDoor = medewerkerId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Transactie succesvol bevestigd";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"Fout bij bevestigen: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Medewerker/Afwijs/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Afwijs(int id, string reden)
        {
            var transactie = await _context.Transacties
                .FirstOrDefaultAsync(t => t.Id == id && t.Deleted == DateTime.MaxValue);

            if (transactie == null || transactie.Status != TransactieStatus.Wachtend)
            {
                return NotFound();
            }

            string medewerkerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;

            transactie.Status = TransactieStatus.Afgewezen;
            transactie.AfwijzingsReden = reden;
            transactie.BevestigdOp = DateTime.Now;
            transactie.BevestigdDoor = medewerkerId;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Transactie afgewezen";
            return RedirectToAction(nameof(Index));
        }

        // GET: Medewerker/Berichten - Overzicht klantenberichten
        public async Task<IActionResult> Berichten()
        {
            var berichten = await _context.KlantBerichten
                .Where(b => b.Deleted == DateTime.MaxValue)
                .Include(b => b.Gebruiker)
                .OrderByDescending(b => b.Datum)
                .ToListAsync();

            return View(berichten);
        }

        // GET: Medewerker/BerichtDetails/5
        public async Task<IActionResult> BerichtDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bericht = await _context.KlantBerichten
                .Include(b => b.Gebruiker)
                .FirstOrDefaultAsync(m => m.Id == id && m.Deleted == DateTime.MaxValue);

            if (bericht == null)
            {
                return NotFound();
            }

            return View(bericht);
        }

        // POST: Medewerker/MarkeerAlsBehandeld/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkeerAlsBehandeld(int id)
        {
            var bericht = await _context.KlantBerichten
                .FirstOrDefaultAsync(b => b.Id == id && b.Deleted == DateTime.MaxValue);

            if (bericht == null)
            {
                return NotFound();
            }

            string medewerkerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;

            bericht.Status = "Afgehandeld";
            bericht.BehandeldDoor = medewerkerId;
            bericht.BehandeldOp = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Bericht gemarkeerd als afgehandeld";
            return RedirectToAction(nameof(Berichten));
        }
    }
}
