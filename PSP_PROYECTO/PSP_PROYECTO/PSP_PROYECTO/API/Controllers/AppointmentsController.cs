using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Models;
using API.Services;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppointmentContext _context;

        public AppointmentsController(AppointmentContext context)
        {
            _context = context;
        }

        // GET: api/Appointments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments()
        {
            return await _context.Appointments.ToListAsync();
        }

        // GET: api/Appointments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Appointment>> GetAppointment(long id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            return appointment;
        }

        // GET: api/Appointments/Available/{date}
        [HttpGet("Available/{date}")]
        public async Task<ActionResult<IEnumerable<DateTime>>> GetAvailableSlots(DateTime date)
        {
            // Get all appointments for the specified date
            var dayAppointments = await _context.Appointments
                .Where(a => a.AppointmentDateTime.Date == date.Date)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();

            // Create a list of all possible time slots (e.g., every 30 minutes from 9 AM to 5 PM)
            var allSlots = new List<DateTime>();
            DateTime startTime = date.Date.AddHours(9); // 9 AM
            DateTime endTime = date.Date.AddHours(17);  // 5 PM

            while (startTime < endTime)
            {
                allSlots.Add(startTime);
                startTime = startTime.AddMinutes(30); // 30-minute appointments
            }

            // Remove the slots that are already booked
            foreach (var appointment in dayAppointments)
            {
                var slotToRemove = allSlots.FirstOrDefault(s => 
                    s >= appointment.AppointmentDateTime && 
                    s < appointment.AppointmentDateTime.AddMinutes(appointment.DurationMinutes));
                
                if (slotToRemove != default)
                {
                    allSlots.Remove(slotToRemove);
                }
            }

            return allSlots;
        }

        // PUT: api/Appointments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAppointment(long id, Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return BadRequest();
            }

            _context.Entry(appointment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                
                // Notify all connected clients about the update
                await AppointmentNotificationService.NotifyAppointmentUpdatedAsync(appointment);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppointmentExists(id))
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

        // POST: api/Appointments
        [HttpPost]
        public async Task<ActionResult<Appointment>> PostAppointment(Appointment appointment)
        {
            // Check if the time slot is already taken
            bool isTimeSlotTaken = await _context.Appointments
                .AnyAsync(a => a.AppointmentDateTime <= appointment.AppointmentDateTime.AddMinutes(appointment.DurationMinutes) &&
                               appointment.AppointmentDateTime <= a.AppointmentDateTime.AddMinutes(a.DurationMinutes));

            if (isTimeSlotTaken)
            {
                return BadRequest("The selected time slot is already booked.");
            }

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Notify all connected clients about the new appointment
            await AppointmentNotificationService.NotifyAppointmentCreatedAsync(appointment);

            return CreatedAtAction("GetAppointment", new { id = appointment.Id }, appointment);
        }

        // DELETE: api/Appointments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(long id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            // Notify all connected clients about the deletion
            await AppointmentNotificationService.NotifyAppointmentDeletedAsync(new { Id = id });

            return NoContent();
        }

        private bool AppointmentExists(long id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        // GET: api/Appointments/Patient/{name}
        [HttpGet("Patient/{name}")]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetPatientAppointments(string name)
        {
            return await _context.Appointments
                .Where(a => a.PatientName.Contains(name))
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();
        }
    }
} 