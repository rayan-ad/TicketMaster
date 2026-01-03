using TicketMaster.DTOs;
using TicketMaster.Repositories;

namespace TicketMaster.Services
{
    public interface ITicketService
    {
        Task<List<TicketDto>> GetMyTicketsAsync(int userId);
        Task<List<TicketDto>> GetTicketsByReservationAsync(int reservationId, int userId);
        Task<TicketDto?> GetTicketByIdAsync(int ticketId, int userId);
        Task<TicketDto?> GetTicketByNumberAsync(string ticketNumber);
        Task<bool> ValidateTicketAsync(string ticketNumber);
    }

    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepo;

        public TicketService(ITicketRepository ticketRepo)
        {
            _ticketRepo = ticketRepo;
        }

        /// <summary>
        /// Récupère tous les billets d'un utilisateur
        /// </summary>
        public async Task<List<TicketDto>> GetMyTicketsAsync(int userId)
        {
            var tickets = await _ticketRepo.GetByUserIdAsync(userId);
            return tickets.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Récupère tous les billets pour une réservation spécifique
        /// </summary>
        public async Task<List<TicketDto>> GetTicketsByReservationAsync(int reservationId, int userId)
        {
            var tickets = await _ticketRepo.GetByReservationIdAsync(reservationId);

            // Vérifier que la réservation appartient à l'utilisateur
            if (tickets.Any() && tickets.First().Reservation.UserId != userId)
            {
                return new List<TicketDto>();
            }

            return tickets.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Récupère un billet par ID si il appartient à l'utilisateur
        /// </summary>
        public async Task<TicketDto?> GetTicketByIdAsync(int ticketId, int userId)
        {
            var ticket = await _ticketRepo.GetByIdAsync(ticketId);
            if (ticket == null || ticket.Reservation.UserId != userId)
            {
                return null;
            }
            return MapToDto(ticket);
        }

        /// <summary>
        /// Récupère un billet par son numéro
        /// </summary>
        public async Task<TicketDto?> GetTicketByNumberAsync(string ticketNumber)
        {
            var ticket = await _ticketRepo.GetByTicketNumberAsync(ticketNumber);
            return ticket != null ? MapToDto(ticket) : null;
        }

        /// <summary>
        /// Valide et marque un billet comme utilisé (scan à l'entrée)
        /// </summary>
        public async Task<bool> ValidateTicketAsync(string ticketNumber)
        {
            var ticket = await _ticketRepo.GetByTicketNumberAsync(ticketNumber);
            if (ticket == null || ticket.IsUsed)
            {
                return false;
            }

            ticket.IsUsed = true;
            ticket.UsedAt = DateTime.UtcNow;
            await _ticketRepo.UpdateAsync(ticket);

            return true;
        }

        /// <summary>
        /// Mappe Ticket vers TicketDto
        /// </summary>
        private TicketDto MapToDto(Models.Ticket ticket)
        {
            return new TicketDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                QrCodeUrl = ticket.QrCodeUrl,
                QrCodeData = ticket.QrCodeData,
                GeneratedAt = ticket.GeneratedAt,
                IsUsed = ticket.IsUsed,
                SeatId = ticket.SeatId,
                Row = ticket.Seat.Row,
                Number = ticket.Seat.Number,
                ZoneName = ticket.Seat.PricingZone?.Name ?? "",
                Price = ticket.Seat.PricingZone?.Price ?? 0,
                EventName = ticket.Reservation.Event.Name,
                EventDate = ticket.Reservation.Event.Date
            };
        }
    }
}
