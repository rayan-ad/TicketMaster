using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketMaster.Models
{
    /// <summary>
    /// Represents a pricing zone within a venue.
    /// Groups seats with the same price and visual identification.
    /// Examples: "VIP", "Standard", "Economy".
    /// </summary>
    public class PricingZone
    {
        /// <summary>
        /// Unique identifier of the pricing zone.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the pricing zone.
        /// Examples: "VIP", "Standard", "Economy".
        /// Maximum length: 100 characters.
        /// </summary>
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Price per seat in this pricing zone.
        /// </summary>
        [Range(0, 999999)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        /// <summary>
        /// Hexadecimal color code for visual identification in the UI.
        /// Default: "#999999".
        /// Maximum length: 9 characters (e.g., "#RRGGBBAA").
        /// </summary>
        [MaxLength(9)]
        public string Color { get; set; } = "#999999";

        /// <summary>
        /// ID of the venue this pricing zone belongs to.
        /// </summary>
        public int VenueId { get; set; }

        /// <summary>
        /// Reference to the venue.
        /// </summary>
        public Venue Venue { get; set; } = null!;
    }
}
