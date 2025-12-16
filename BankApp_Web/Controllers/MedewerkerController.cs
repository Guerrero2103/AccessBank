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
        public async Task<IActionResult> Index()
        {
            var wachtendeTransacties = _context.Transacties
                .Where(t => t.Deleted == DateTime.MaxValue && t.Status == TransactieStatus.Wachtend)
                .Include(t => t.Gebruiker)
                .ThenInclude(g => g.Adres)
                .OrderByDescending(t => t.Datum);

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
    }
}
