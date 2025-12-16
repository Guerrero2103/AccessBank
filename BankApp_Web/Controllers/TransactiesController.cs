using BankApp_Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_Web.Controllers
{
    [Authorize]
    public class TransactiesController : Controller
    {
        private readonly AppDbContext _context;

        public TransactiesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Transacties
        public async Task<IActionResult> Index(string sortOrder, string filterStatus, int page = 1, int pageSize = 10)
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
            ViewData["CurrentFilter"] = filterStatus;
            if (!string.IsNullOrEmpty(filterStatus))
            {
                if (Enum.TryParse<TransactieStatus>(filterStatus, out var status))
                {
                    transacties = transacties.Where(t => t.Status == status);
                }
            }

            // Sortering
            ViewData["DatumSortParm"] = sortOrder == "Datum" ? "datum_desc" : "Datum";
            ViewData["BedragSortParm"] = sortOrder == "Bedrag" ? "bedrag_desc" : "Bedrag";
            ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";

            switch (sortOrder)
            {
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

            // Status filter dropdown
            ViewBag.StatusFilter = new SelectList(
                Enum.GetValues(typeof(TransactieStatus)).Cast<TransactieStatus>(),
                filterStatus
            );

            int totalCount = await transacties.CountAsync();
            var results = await transacties.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View(results);
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
            
            // Haal rekeningen op van gebruiker voor dropdown
            var rekeningen = _context.Rekeningen
                .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                .ToList();

            ViewData["VanIban"] = new SelectList(rekeningen, "Iban", "Iban");
            ViewData["GebruikerId"] = gebruikerId;

            return View();
        }

        // POST: Transacties/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VanIban,NaarIban,NaamOntvanger,Bedrag,Omschrijving,GebruikerId")] Transactie transactie)
        {
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

                    if (vanRekening != null && naarRekening != null && vanRekening.Saldo >= transactie.Bedrag)
                    {
                        vanRekening.Saldo -= transactie.Bedrag;
                        naarRekening.Saldo += transactie.Bedrag;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Onvoldoende saldo of rekening niet gevonden");
                        return View(transactie);
                    }
                }

                _context.Add(transactie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;
            var rekeningen = _context.Rekeningen
                .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                .ToList();
            ViewData["VanIban"] = new SelectList(rekeningen, "Iban", "Iban", transactie.VanIban);

            return View(transactie);
        }

        private bool TransactieExists(int id)
        {
            return _context.Transacties.Any(e => e.Id == id && e.Deleted == DateTime.MaxValue);
        }
    }
}
