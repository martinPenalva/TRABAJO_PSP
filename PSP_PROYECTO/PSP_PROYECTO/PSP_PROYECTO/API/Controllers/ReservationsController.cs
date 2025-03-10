using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly RestaurantContext _context;

        public ReservationsController(RestaurantContext context)
        {
            _context = context;
        }

        // GET: api/Reservations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Reservation>>> GetReservations()
        {
            return await _context.Reservations.ToListAsync();
        }

        // GET: api/Reservations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Reservation>> GetReservation(long id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            return reservation;
        }

        // GET: api/Reservations/Available/2024-03-20
        [HttpGet("Available/{date}")]
        public async Task<ActionResult<IEnumerable<int>>> GetAvailableTables(DateTime date)
        {
            var reservedTables = await _context.Reservations
                .Where(r => r.ReservationDateTime.Date == date.Date)
                .Select(r => r.TableNumber)
                .ToListAsync();

            var allTables = Enumerable.Range(1, 20); // Assuming 20 tables
            var availableTables = allTables.Except(reservedTables);

            return Ok(availableTables);
        }

        // POST: api/Reservations
        [HttpPost]
        public async Task<ActionResult<Reservation>> CreateReservation(Reservation reservation)
        {
            // Check if table is available for the given date and time
            var isTableAvailable = !await _context.Reservations
                .AnyAsync(r => r.TableNumber == reservation.TableNumber && 
                              r.ReservationDateTime == reservation.ReservationDateTime);

            if (!isTableAvailable)
            {
                return BadRequest("Table is not available for the selected date and time.");
            }

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, reservation);
        }

        // PUT: api/Reservations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReservation(long id, Reservation reservation)
        {
            if (id != reservation.Id)
            {
                return BadRequest();
            }

            // Check if table is available for the new date and time
            var isTableAvailable = !await _context.Reservations
                .AnyAsync(r => r.TableNumber == reservation.TableNumber && 
                              r.ReservationDateTime == reservation.ReservationDateTime &&
                              r.Id != id);

            if (!isTableAvailable)
            {
                return BadRequest("Table is not available for the selected date and time.");
            }

            reservation.LastModifiedAt = DateTime.UtcNow;
            _context.Entry(reservation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationExists(id))
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

        // DELETE: api/Reservations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation(long id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ReservationExists(long id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }
    }
} 