using TicketMaster.Enum;

namespace TicketMaster.Models
{
    /// <summary>
    /// Represents a seat template in a venue.
    /// A seat is reusable across all events in a venue.
    /// The state of a seat (available, reserved, sold) is tracked per event in SeatReservationState.
    /// </summary>
    public class Seat
    {
        /// <summary>
        /// Unique identifier of the seat.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Row identifier for the seat (e.g., "A", "B", "V1").
        /// </summary>
        public string Row { get; set; } = string.Empty;

        /// <summary>
        /// Seat number within the row.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// ID of the pricing zone this seat belongs to.
        /// </summary>
        public int PricingZoneId { get; set; }

        /// <summary>
        /// Reference to the pricing zone.
        /// Defines the price for this seat.
        /// </summary>
        public PricingZone PricingZone { get; set; } = null!;

        /// <summary>
        /// Collection of reservation seats linking this seat to reservations.
        /// </summary>
        public ICollection<ReservationSeat> ReservationSeats { get; set; } = new List<ReservationSeat>();

        /// <summary>
        /// Collection of seat reservation states per event.
        /// Tracks whether the seat is available, held, reserved, or sold for each event.
        /// </summary>
        public ICollection<SeatReservationState> ReservationStates { get; set; } = new List<SeatReservationState>();
    }
}
