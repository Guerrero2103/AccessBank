using BankApp_Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<BankUser> _userManager;

        public AdminController(AppDbContext context, UserManager<BankUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin - Overzicht gebruikers
        public async Task<IActionResult> Index()
        {
            var gebruikers = _context.Users
                .Where(u => u.Deleted == DateTime.MaxValue)
                .Include(u => u.Adres)
                .ToListAsync();

            return View(await gebruikers);
        }

        // GET: Admin/Gebruikers/Details/5
        public async Task<IActionResult> GebruikerDetails(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gebruiker = await _context.Users
                .Include(u => u.Adres)
                .Include(u => u.Rekeningen)
                .Include(u => u.Kaarten)
                .FirstOrDefaultAsync(m => m.Id == id && m.Deleted == DateTime.MaxValue);

            if (gebruiker == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(gebruiker);
            ViewBag.Roles = roles;

            return View(gebruiker);
        }

        // GET: Admin/Gebruikers/Blokkeer/5
        public async Task<IActionResult> BlokkeerGebruiker(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gebruiker = await _context.Users.FindAsync(id);
            if (gebruiker == null || gebruiker.Deleted != DateTime.MaxValue)
            {
                return NotFound();
            }

            return View(gebruiker);
        }

        // POST: Admin/Gebruikers/Blokkeer/5
        [HttpPost, ActionName("BlokkeerGebruiker")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlokkeerGebruikerConfirmed(string id)
        {
            var gebruiker = await _context.Users.FindAsync(id);
            if (gebruiker != null)
            {
                gebruiker.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                gebruiker.LockoutEnabled = true;
                await _userManager.UpdateAsync(gebruiker);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Gebruikers/DeBlokkeer/5
        public async Task<IActionResult> DeBlokkeerGebruiker(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gebruiker = await _context.Users.FindAsync(id);
            if (gebruiker == null || gebruiker.Deleted != DateTime.MaxValue)
            {
                return NotFound();
            }

            gebruiker.LockoutEnd = null;
            gebruiker.LockoutEnabled = false;
            await _userManager.UpdateAsync(gebruiker);

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Rollen
        public async Task<IActionResult> Rollen(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gebruiker = await _context.Users.FindAsync(id);
            if (gebruiker == null || gebruiker.Deleted != DateTime.MaxValue)
            {
                return NotFound();
            }

            var gebruikerRoles = await _userManager.GetRolesAsync(gebruiker);
            var alleRoles = _context.Roles.ToList();

            ViewBag.GebruikerId = id;
            ViewBag.GebruikerNaam = gebruiker.UserName;
            ViewBag.GebruikerRoles = gebruikerRoles;
            ViewBag.AlleRoles = alleRoles;

            return View();
        }

        // POST: Admin/Rollen/Toevoegen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoegRolToe(string gebruikerId, string rolNaam)
        {
            var gebruiker = await _context.Users.FindAsync(gebruikerId);
            if (gebruiker == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsInRoleAsync(gebruiker, rolNaam))
            {
                await _userManager.AddToRoleAsync(gebruiker, rolNaam);
            }

            return RedirectToAction(nameof(Rollen), new { id = gebruikerId });
        }

        // POST: Admin/Rollen/Verwijderen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerwijderRol(string gebruikerId, string rolNaam)
        {
            var gebruiker = await _context.Users.FindAsync(gebruikerId);
            if (gebruiker == null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(gebruiker, rolNaam))
            {
                await _userManager.RemoveFromRoleAsync(gebruiker, rolNaam);
            }

            return RedirectToAction(nameof(Rollen), new { id = gebruikerId });
        }
    }
}
