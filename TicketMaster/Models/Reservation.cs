using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMaster.Models
{
    /// <summary>
    /// Enumeration of reservation statuses.
    /// </summary>
    public enum ReservationStatus
    {
        /// <summary>
        /// Reservation is pending payment. Seats are temporarily held for a limited time.
        /// </summary>
        Pending,

        /// <summary>
        /// Reservation has been paid. Seats are confirmed and tickets generated.
        /// </summary>
        Paid,

        /// <summary>
        /// Reservation has been canceled. Seats are released.
        /// </summary>
        Canceled
    }

    /// <summary>
    /// Represents a reservation made by a user for an event.
    /// Manages the collection of selected seats, payment information, and generated tickets.
    /// </summary>
    public class Reservation
    {
        /// <summary>
        /// Unique identifier of the reservation.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID of the user who made the reservation.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Reference to the user who made the reservation.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// ID of the event for which the reservation is made.
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// Reference to the event.
        /// </summary>
        public Event Event { get; set; } = null!;

        /// <summary>
        /// Current status of the reservation.
        /// Defaults to Pending (seats held temporarily).
        /// </summary>
        [Required]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        /// <summary>
        /// Timestamp when the reservation was created.
        /// Used to calculate expiration time for temporary holds.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Expiration timestamp for the temporary hold on seats.
        /// If this time is exceeded and payment is not completed, seats are released.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Total amount to be paid for this reservation.
        /// Calculated from the sum of seat prices.
        /// </summary>
        [Range(0, 999999)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Collection of selected seats for this reservation.
        /// </summary>
        public ICollection<ReservationSeat> ReservationSeats { get; set; } = new List<ReservationSeat>();

        /// <summary>
        /// Collection of payments made for this reservation.
        /// </summary>
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        /// <summary>
        /// Collection of tickets generated after payment confirmation.
        /// One ticket is generated per reserved seat.
        /// </summary>
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
