using BankApp_Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_Web.Controllers
{
    [Authorize(Roles = "Klant,Admin")]
    public class KaartenController : Controller
    {
        private readonly AppDbContext _context;

        public KaartenController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Kaarten
        public async Task<IActionResult> Index()
        {
            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;

            var kaarten = await _context.Kaarten
                .Where(k => k.Deleted == DateTime.MaxValue && k.GebruikerId == gebruikerId)
                .Include(k => k.Gebruiker)
                .OrderBy(k => k.Id)
                .ToListAsync();

            return View(kaarten);
        }

        // GET: Kaarten/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;

            var kaart = await _context.Kaarten
                .Include(k => k.Gebruiker)
                .FirstOrDefaultAsync(m => m.Id == id && m.GebruikerId == gebruikerId && m.Deleted == DateTime.MaxValue);

            if (kaart == null)
            {
                return NotFound();
            }

            return View(kaart);
        }
    }
}
