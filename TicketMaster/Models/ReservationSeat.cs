using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMaster.Models
{
    /// <summary>
    /// Represents the junction between a reservation and a seat.
    /// Stores the price snapshot at the time of booking to preserve historical price information.
    /// </summary>
    public class ReservationSeat
    {
        /// <summary>
        /// ID of the reservation.
        /// Part of the composite primary key.
        /// </summary>
        public int ReservationId { get; set; }

        /// <summary>
        /// Reference to the reservation.
        /// </summary>
        public Reservation Reservation { get; set; } = null!;

        /// <summary>
        /// ID of the seat.
        /// Part of the composite primary key.
        /// </summary>
        public int SeatId { get; set; }

        /// <summary>
        /// Reference to the seat.
        /// </summary>
        public Seat Seat { get; set; } = null!;

        /// <summary>
        /// Price of the seat at the time of booking.
        /// This is a snapshot to preserve the historical price even if pricing changes later.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAtBooking { get; set; }
    }
}
