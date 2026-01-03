using System.ComponentModel.DataAnnotations;

namespace TicketMaster.Models
{
    /// <summary>
    /// Represents a venue where events take place.
    /// Contains information about the venue's capacity, pricing zones, and available seats.
    /// </summary>
    public class Venue
    {
        /// <summary>
        /// Unique identifier of the venue.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the venue.
        /// </summary>
        public String Name { get; set; } = String.Empty;

        /// <summary>
        /// Total seating capacity of the venue.
        /// Must be greater than 0.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Capacity { get; set; }

        /// <summary>
        /// Collection of pricing zones defined for this venue.
        /// Each zone has its own price and seat allocation.
        /// </summary>
        public ICollection<PricingZone> PricingZones { get; set; } = new List<PricingZone>();

        /// <summary>
        /// Collection of all seats available in this venue.
        /// Seats are organized by pricing zones and rows.
        /// </summary>
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    }
}
