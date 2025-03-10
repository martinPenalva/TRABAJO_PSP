namespace API.Models;

public class Appointment
{
    public long Id { get; set; }
    public string? PatientName { get; set; }
    public string? ContactPhone { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string? TreatmentType { get; set; }
    public string? Notes { get; set; }
    public bool IsConfirmed { get; set; }
} 