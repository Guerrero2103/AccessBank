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
    [Authorize(Roles = "Klant,Admin")]
    public class RekeningenController : Controller
    {
        private readonly AppDbContext _context;

        public RekeningenController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Rekeningen
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;

            var query = _context.Rekeningen
                .Where(r => r.Deleted == DateTime.MaxValue && r.GebruikerId == gebruikerId)
                .Include(r => r.Gebruiker)
                .OrderBy(r => r.Id)
                .AsQueryable();

            int totalCount = await query.CountAsync();
            var rekeningen = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View(rekeningen);
        }

        // GET: Rekeningen/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;

            var rekening = await _context.Rekeningen
                .Include(r => r.Gebruiker)
                .FirstOrDefaultAsync(m => m.Id == id && m.GebruikerId == gebruikerId && m.Deleted == DateTime.MaxValue);

            if (rekening == null)
            {
                return NotFound();
            }

            return View(rekening);
        }

        // GET: Rekeningen/Create
        public IActionResult Create()
        {
            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;
            ViewData["GebruikerId"] = gebruikerId;
            return View();
        }

        // POST: Rekeningen/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Iban,Saldo,GebruikerId")] Rekening rekening)
        {
            if (ModelState.IsValid)
            {
                rekening.Deleted = DateTime.MaxValue;
                _context.Add(rekening);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(rekening);
        }

        // GET: Rekeningen/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rekening = await _context.Rekeningen.FindAsync(id);
            if (rekening == null || rekening.Deleted != DateTime.MaxValue)
            {
                return NotFound();
            }
            return View(rekening);
        }

        // POST: Rekeningen/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Iban,Saldo,GebruikerId")] Rekening rekening)
        {
            if (id != rekening.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    rekening.Deleted = DateTime.MaxValue;
                    _context.Update(rekening);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RekeningExists(rekening.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(rekening);
        }

        // GET: Rekeningen/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rekening = await _context.Rekeningen
                .Include(r => r.Gebruiker)
                .FirstOrDefaultAsync(m => m.Id == id && m.Deleted == DateTime.MaxValue);

            if (rekening == null)
            {
                return NotFound();
            }

            return View(rekening);
        }

        // POST: Rekeningen/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rekening = await _context.Rekeningen.FindAsync(id);
            if (rekening != null)
            {
                rekening.Deleted = DateTime.Now;
                _context.Rekeningen.Update(rekening);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Rekeningen/GetSaldo - Ajax endpoint
        [HttpGet("GetSaldo")]
        public async Task<IActionResult> GetSaldo()
        {
            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;
            
            var totaalSaldo = await _context.Rekeningen
                .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                .SumAsync(r => r.Saldo);

            return Json(totaalSaldo.ToString("F2"));
        }

        private bool RekeningExists(int id)
        {
            return _context.Rekeningen.Any(e => e.Id == id && e.Deleted == DateTime.MaxValue);
        }
    }
}
