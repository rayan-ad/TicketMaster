using Microsoft.EntityFrameworkCore;
using TicketMaster.DataAccess;
using TicketMaster.Models;

namespace TicketMaster.Repositories
{
    public interface ITicketRepository
    {
        Task<Ticket?> GetByIdAsync(int id);
        Task<Ticket?> GetByTicketNumberAsync(string ticketNumber);
        Task<List<Ticket>> GetByReservationIdAsync(int reservationId);
        Task<List<Ticket>> GetByUserIdAsync(int userId);
        Task<Ticket> CreateAsync(Ticket ticket);
        Task UpdateAsync(Ticket ticket);
    }

    public class TicketRepository : ITicketRepository
    {
        private readonly TicketMasterContext _context;

        public TicketRepository(TicketMasterContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Récupère un billet par ID avec toutes les relations
        /// </summary>
        public async Task<Ticket?> GetByIdAsync(int id)
        {
            return await _context.Tickets
                .Include(t => t.Reservation)
                    .ThenInclude(r => r.Event)
                .Include(t => t.Seat)
                    .ThenInclude(s => s.PricingZone)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// Récupère un billet par son numéro unique
        /// </summary>
        public async Task<Ticket?> GetByTicketNumberAsync(string ticketNumber)
        {
            return await _context.Tickets
                .Include(t => t.Reservation)
                    .ThenInclude(r => r.Event)
                .Include(t => t.Seat)
                    .ThenInclude(s => s.PricingZone)
                .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);
        }

        /// <summary>
        /// Récupère tous les billets d'une réservation
        /// </summary>
        public async Task<List<Ticket>> GetByReservationIdAsync(int reservationId)
        {
            return await _context.Tickets
                .Include(t => t.Seat)
                    .ThenInclude(s => s.PricingZone)
                .Include(t => t.Reservation)
                    .ThenInclude(r => r.Event)
                .Where(t => t.ReservationId == reservationId)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère tous les billets d'un utilisateur
        /// </summary>
        public async Task<List<Ticket>> GetByUserIdAsync(int userId)
        {
            return await _context.Tickets
                .Include(t => t.Reservation)
                    .ThenInclude(r => r.Event)
                .Include(t => t.Seat)
                    .ThenInclude(s => s.PricingZone)
                .Where(t => t.Reservation.UserId == userId)
                .OrderByDescending(t => t.GeneratedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Crée un nouveau billet
        /// </summary>
        public async Task<Ticket> CreateAsync(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }

        /// <summary>
        /// Met à jour un billet existant
        /// </summary>
        public async Task UpdateAsync(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();
        }
    }
}
