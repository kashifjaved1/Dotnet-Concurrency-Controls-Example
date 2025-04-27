using System.ComponentModel.DataAnnotations;

namespace Dotnet_Concurrency_Controls.Data.Entities
{
    public class Booking : DefaultEntity
    {
        [Required]
        [Display(Name = "Guest Name")]
        public string GuestName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Check-in Date")]
        public DateTime CheckInDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Check-out Date")]
        public DateTime CheckOutDate { get; set; }

        // Locking properties
        public string? LockedBy { get; set; }
        public DateTimeOffset? LockExpiry { get; set; }
        public string? LockType { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
