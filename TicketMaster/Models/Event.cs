using System.ComponentModel.DataAnnotations;

namespace TicketMaster.Models
{
    public class Event
    {
        public int Id { get; set; }
        [MaxLength(200)]
        public string Name { get; set; } = String.Empty;
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;

        public string? ImageEvent { get; set; }
    }
}
