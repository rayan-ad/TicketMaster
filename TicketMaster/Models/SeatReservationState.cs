using System.ComponentModel.DataAnnotations;
using TicketMaster.Enum;

namespace TicketMaster.Models
{
    /// <summary>
    /// État d'un siège pour un événement spécifique
    /// POURQUOI CETTE TABLE ?
    /// - Un siège peut être libre pour Event 1 et réservé pour Event 2
    /// - Chaque event a son propre état de sièges
    /// - Les sièges deviennent des "templates" réutilisables
    /// </summary>
    public class SeatReservationState
    {
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }
        public Event Event { get; set; } = null!;

        [Required]
        public int SeatId { get; set; }
        public Seat Seat { get; set; } = null!;

        [Required]
        public SeatStatus State { get; set; } = SeatStatus.Free;

        public int? UserId { get; set; }
        public User? User { get; set; }

        public DateTime? ReservedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
