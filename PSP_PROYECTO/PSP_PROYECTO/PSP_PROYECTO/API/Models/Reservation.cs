using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class Reservation
    {
        public long Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime ReservationDateTime { get; set; }
        
        [Required]
        [Range(1, 20)]
        public int NumberOfGuests { get; set; }
        
        [StringLength(500)]
        public string? SpecialRequests { get; set; }
        
        [Required]
        public int TableNumber { get; set; }
        
        public bool IsConfirmed { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastModifiedAt { get; set; }
    }
} 