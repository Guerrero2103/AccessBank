using BankApp_Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_Web.Controllers
{
    [Authorize]
    public class TransactiesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TransactiesController> _logger;

        public TransactiesController(AppDbContext context, ILogger<TransactiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Transacties
        public async Task<IActionResult> Index(string sortOrder, string filterStatus, int page = 1, int pageSize = 10)
        {
            try
            {
                string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;

                // Haal IBANs op van gebruiker
                var gebruikerIbans = await _context.Rekeningen
                    .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                    .Select(r => r.Iban)
                    .ToListAsync();

                var transacties = _context.Transacties
                    .Where(t => t.Deleted == DateTime.MaxValue &&
                               (gebruikerIbans.Contains(t.VanIban) || gebruikerIbans.Contains(t.NaarIban)))
                    .Include(t => t.Gebruiker)
                    .AsQueryable();

                // Filter op status
                ViewData["CurrentFilter"] = filterStatus ?? "";
                if (!string.IsNullOrEmpty(filterStatus))
                {
                    if (Enum.TryParse<TransactieStatus>(filterStatus, out var status))
                    {
                        transacties = transacties.Where(t => t.Status == status);
                    }
                }

                // Sortering - standaard op datum als sortOrder leeg is
                if (string.IsNullOrEmpty(sortOrder))
                {
                    sortOrder = "datum_desc";
                }

                ViewData["DatumSortParm"] = sortOrder == "Datum" ? "datum_desc" : "Datum";
                ViewData["BedragSortParm"] = sortOrder == "Bedrag" ? "bedrag_desc" : "Bedrag";
                ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";

                switch (sortOrder)
                {
                    case "Datum":
                        transacties = transacties.OrderBy(t => t.Datum);
                        break;
                    case "datum_desc":
                        transacties = transacties.OrderByDescending(t => t.Datum);
                        break;
                    case "Bedrag":
                        transacties = transacties.OrderBy(t => t.Bedrag);
                        break;
                    case "bedrag_desc":
                        transacties = transacties.OrderByDescending(t => t.Bedrag);
                        break;
                    case "Status":
                        transacties = transacties.OrderBy(t => t.Status);
                        break;
                    case "status_desc":
                        transacties = transacties.OrderByDescending(t => t.Status);
                        break;
                    default:
                        transacties = transacties.OrderByDescending(t => t.Datum);
                        break;
                }

                // Status filter wordt nu handmatig in de view gemaakt

                int totalCount = await transacties.CountAsync();
                var results = await transacties.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;

                return View(results);
            }
            catch (Exception ex)
            {
                // Log de fout met volledige details
                _logger.LogError(ex, "Fout bij ophalen transacties. SortOrder: {SortOrder}, FilterStatus: {FilterStatus}, Error: {Error}", 
                    sortOrder, filterStatus, ex.Message);
                
                // Toon een lege lijst in plaats van een error pagina
                ViewBag.Page = 1;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = 0;
                ViewData["DatumSortParm"] = "datum_desc";
                ViewData["BedragSortParm"] = "Bedrag";
                ViewData["StatusSortParm"] = "Status";
                ViewData["CurrentFilter"] = filterStatus ?? "";
                
                return View(new List<Transactie>());
            }
        }

        // Partial view: recent transactions (used by AJAX)
        [HttpGet]
        public async Task<IActionResult> RecentPartial(int count = 5)
        {
            if (!User.Identity.IsAuthenticated)
                return PartialView("_RecentTransactionsPartial", Enumerable.Empty<Transactie>());

            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;

            var gebruikerIbans = await _context.Rekeningen
                .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                .Select(r => r.Iban)
                .ToListAsync();

            var recent = await _context.Transacties
                .Where(t => t.Deleted == DateTime.MaxValue && (gebruikerIbans.Contains(t.VanIban) || gebruikerIbans.Contains(t.NaarIban)))
                .OrderByDescending(t => t.Datum)
                .Take(count)
                .ToListAsync();

            return PartialView("_RecentTransactionsPartial", recent);
        }

        // GET: Transacties/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;
            var gebruikerIbans = await _context.Rekeningen
                .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                .Select(r => r.Iban)
                .ToListAsync();

            var transactie = await _context.Transacties
                .Include(t => t.Gebruiker)
                .FirstOrDefaultAsync(m => m.Id == id && 
                    m.Deleted == DateTime.MaxValue &&
                    (gebruikerIbans.Contains(m.VanIban) || gebruikerIbans.Contains(m.NaarIban)));

            if (transactie == null)
            {
                return NotFound();
            }

            return View(transactie);
        }

        // GET: Transacties/Create
        public IActionResult Create()
        {
            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;
            
            // Haal rekeningen op van gebruiker voor dropdown - gebruik direct string lijst
            var ibanLijst = _context.Rekeningen
                .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                .Select(r => r.Iban)
                .ToList();

            // Gebruik direct de IBAN lijst in plaats van SelectList
            ViewBag.VanIbanLijst = ibanLijst;
            ViewData["GebruikerId"] = gebruikerId;

            return View();
        }

        // POST: Transacties/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VanIban,NaarIban,NaamOntvanger,Bedrag,Omschrijving")] Transactie transactie)
        {
            // Haal gebruiker ID op
            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;
            transactie.GebruikerId = gebruikerId;

            if (ModelState.IsValid)
            {
                transactie.Datum = DateTime.Now;
                transactie.Status = transactie.Bedrag >= 500 ? TransactieStatus.Wachtend : TransactieStatus.Voltooid;
                transactie.Deleted = DateTime.MaxValue;

                // Als bedrag < 500€, direct uitvoeren
                if (transactie.Status == TransactieStatus.Voltooid)
                {
                    var vanRekening = await _context.Rekeningen
                        .FirstOrDefaultAsync(r => r.Iban == transactie.VanIban && r.Deleted == DateTime.MaxValue);
                    var naarRekening = await _context.Rekeningen
                        .FirstOrDefaultAsync(r => r.Iban == transactie.NaarIban && r.Deleted == DateTime.MaxValue);

                    if (vanRekening == null)
                    {
                        ModelState.AddModelError("VanIban", "Bronrekening niet gevonden");
                    }
                    else if (naarRekening == null)
                    {
                        ModelState.AddModelError("NaarIban", "Doelrekening niet gevonden");
                    }
                    else if (vanRekening.Saldo < transactie.Bedrag)
                    {
                        ModelState.AddModelError("Bedrag", "Onvoldoende saldo op rekening");
                    }
                    else
                    {
                        // Geld overmaken
                        vanRekening.Saldo -= transactie.Bedrag;
                        naarRekening.Saldo += transactie.Bedrag;
                        _context.Rekeningen.Update(vanRekening);
                        _context.Rekeningen.Update(naarRekening);
                    }

                    // Als er fouten zijn, toon view met fouten
                    if (!ModelState.IsValid)
                    {
                        var ibanLijst = _context.Rekeningen
                            .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                            .Select(r => r.Iban)
                            .ToList();
                        ViewBag.VanIbanLijst = ibanLijst;
                        return View(transactie);
                    }
                }

                _context.Add(transactie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Herlaad IBAN lijst bij fout
            var ibanLijstError = _context.Rekeningen
                .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                .Select(r => r.Iban)
                .ToList();

            ViewBag.VanIbanLijst = ibanLijstError;

            return View(transactie);
        }

        private bool TransactieExists(int id)
        {
            return _context.Transacties.Any(e => e.Id == id && e.Deleted == DateTime.MaxValue);
        }
    }
}
