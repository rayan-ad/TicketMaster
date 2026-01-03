using System.ComponentModel.DataAnnotations;
using TicketMaster.Models;

namespace TicketMaster.DTOs
{
    /// <summary>
    /// DTO pour créer une nouvelle réservation
    /// </summary>
    public class CreateReservationDto
    {
        [Required]
        public int EventId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Au moins un siège doit être sélectionné")]
        public List<int> SeatIds { get; set; } = new();
    }

    /// <summary>
    /// DTO de réponse pour une réservation
    /// </summary>
    public class ReservationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public List<ReservationSeatDto> Seats { get; set; } = new();
        public List<TicketDto>? Tickets { get; set; }
    }

    /// <summary>
    /// DTO pour un siège dans une réservation
    /// </summary>
    public class ReservationSeatDto
    {
        public int SeatId { get; set; }
        public string Row { get; set; } = string.Empty;
        public int Number { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public decimal PriceAtBooking { get; set; }
    }

    /// <summary>
    /// DTO pour un billet électronique
    /// </summary>
    public class TicketDto
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
        public string QrCodeData { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public bool IsUsed { get; set; }
        public int SeatId { get; set; }
        public string Row { get; set; } = string.Empty;
        public int Number { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
    }
}
