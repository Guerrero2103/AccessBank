using BankApp_Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankApp_Web.API_Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Klant,Admin")]
    public class RekeningenController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RekeningenController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Rekeningen
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Rekening>>> GetRekeningen()
        {
            // Gebruik NameIdentifier claim (userId) of fallback naar Name (userName)
            string? userName = User.Identity?.Name;
            string? userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            string gebruikerId;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                gebruikerId = userIdClaim;
            }
            else if (!string.IsNullOrEmpty(userName))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
                gebruikerId = user?.Id ?? "";
            }
            else
            {
                return Unauthorized();
            }
            
            var rekeningen = await _context.Rekeningen
                .Where(r => r.Deleted == DateTime.MaxValue && r.GebruikerId == gebruikerId)
                // .Include(r => r.Gebruiker)  // ← Verwijderd: MAUI app heeft Gebruiker niet nodig
                .ToListAsync();

            return rekeningen;
        }

        // GET: api/Rekeningen/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Rekening>> GetRekening(int id)
        {
            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;

            var rekening = await _context.Rekeningen
                // .Include(r => r.Gebruiker)  // ← Verwijderd: MAUI app heeft Gebruiker niet nodig
                .FirstOrDefaultAsync(r => r.Id == id && r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue);

            if (rekening == null)
            {
                return NotFound();
            }

            return rekening;
        }

        // POST: api/Rekeningen
        [HttpPost]
        public async Task<ActionResult<Rekening>> PostRekening(Rekening rekening)
        {
            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;
            rekening.GebruikerId = gebruikerId;
            rekening.Deleted = DateTime.MaxValue;

            _context.Rekeningen.Add(rekening);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRekening", new { id = rekening.Id }, rekening);
        }

        // PUT: api/Rekeningen/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutRekening(int id, Rekening rekening)
        {
            if (id != rekening.Id)
            {
                return BadRequest();
            }

            rekening.Deleted = DateTime.MaxValue;
            _context.Entry(rekening).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RekeningExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Rekeningen/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRekening(int id)
        {
            var rekening = await _context.Rekeningen.FindAsync(id);
            if (rekening == null)
            {
                return NotFound();
            }

            rekening.Deleted = DateTime.Now;
            _context.Rekeningen.Update(rekening);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RekeningExists(int id)
        {
            return _context.Rekeningen.Any(e => e.Id == id && e.Deleted == DateTime.MaxValue);
        }
    }
}
