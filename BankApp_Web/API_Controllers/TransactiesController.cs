using BankApp_Models;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class TransactiesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactiesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Transacties
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transactie>>> GetTransacties()
        {
            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;

            var gebruikerIbans = await _context.Rekeningen
                .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                .Select(r => r.Iban)
                .ToListAsync();

            var transacties = await _context.Transacties
                .Where(t => t.Deleted == DateTime.MaxValue &&
                           (gebruikerIbans.Contains(t.VanIban) || gebruikerIbans.Contains(t.NaarIban)))
                .Include(t => t.Gebruiker)
                .OrderByDescending(t => t.Datum)
                .ToListAsync();

            return transacties;
        }

        // GET: api/Transacties/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Transactie>> GetTransactie(int id)
        {
            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;
            var gebruikerIbans = await _context.Rekeningen
                .Where(r => r.GebruikerId == gebruikerId && r.Deleted == DateTime.MaxValue)
                .Select(r => r.Iban)
                .ToListAsync();

            var transactie = await _context.Transacties
                .Include(t => t.Gebruiker)
                .FirstOrDefaultAsync(t => t.Id == id && 
                    t.Deleted == DateTime.MaxValue &&
                    (gebruikerIbans.Contains(t.VanIban) || gebruikerIbans.Contains(t.NaarIban)));

            if (transactie == null)
            {
                return NotFound();
            }

            return transactie;
        }

        // POST: api/Transacties
        [HttpPost]
        public async Task<ActionResult<Transactie>> PostTransactie(Transactie transactie)
        {
            string gebruikerId = _context.Users.First(u => u.UserName == User.Identity.Name).Id;
            transactie.GebruikerId = gebruikerId;
            transactie.Datum = DateTime.Now;
            transactie.Status = transactie.Bedrag >= 500 ? TransactieStatus.Wachtend : TransactieStatus.Voltooid;
            transactie.Deleted = DateTime.MaxValue;

            // Bij bedrag onder 500 euro: direct geld overmaken
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
                    return BadRequest("Onvoldoende saldo of rekening niet gevonden");
                }
            }

            _context.Transacties.Add(transactie);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTransactie", new { id = transactie.Id }, transactie);
        }

        // GET: api/Transacties/Wachtend
        [HttpGet("Wachtend")]
        [Authorize(Roles = "Medewerker,Admin")]
        public async Task<ActionResult<IEnumerable<Transactie>>> GetWachtendeTransacties()
        {
            var transacties = await _context.Transacties
                .Where(t => t.Deleted == DateTime.MaxValue && t.Status == TransactieStatus.Wachtend)
                .Include(t => t.Gebruiker)
                .ThenInclude(g => g.Adres)
                .OrderByDescending(t => t.Datum)
                .ToListAsync();

            return transacties;
        }

        // PUT: api/Transacties/5/Bevestig
        [HttpPut("{id}/Bevestig")]
        [Authorize(Roles = "Medewerker,Admin")]
        public async Task<IActionResult> BevestigTransactie(int id)
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
                return BadRequest("Onvoldoende saldo op rekening");
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                vanRekening.Saldo -= transactie.Bedrag;
                naarRekening.Saldo += transactie.Bedrag;

                transactie.Status = TransactieStatus.Voltooid;
                transactie.BevestigdOp = DateTime.Now;
                transactie.BevestigdDoor = medewerkerId;

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return BadRequest($"Fout bij bevestigen: {ex.Message}");
            }
        }

        // PUT: api/Transacties/5/Afwijs
        [HttpPut("{id}/Afwijs")]
        [Authorize(Roles = "Medewerker,Admin")]
        public async Task<IActionResult> AfwijsTransactie(int id, [FromBody] string reden)
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

            return NoContent();
        }
    }
}
