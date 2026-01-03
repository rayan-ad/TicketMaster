using Microsoft.EntityFrameworkCore;
using TicketMaster.DataAccess;
using TicketMaster.Models;

namespace TicketMaster.Repositories
{
    public interface IReservationRepository
    {
        Task<Reservation?> GetByIdAsync(int id);
        Task<List<Reservation>> GetByUserIdAsync(int userId);
        Task<List<Reservation>> GetByEventIdAsync(int eventId);
        Task<List<Reservation>> GetExpiredReservationsAsync();
        Task<Reservation> CreateAsync(Reservation reservation);
        Task UpdateAsync(Reservation reservation);
        Task DeleteAsync(int id);
    }

    public class ReservationRepository : IReservationRepository
    {
        private readonly TicketMasterContext _context;

        public ReservationRepository(TicketMasterContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Récupère une réservation par ID avec toutes les relations
        /// </summary>
        public async Task<Reservation?> GetByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Event)
                .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                        .ThenInclude(s => s.PricingZone)
                .Include(r => r.Tickets)
                    .ThenInclude(t => t.Seat)
                        .ThenInclude(s => s.PricingZone)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// Récupère toutes les réservations d'un utilisateur
        /// </summary>
        public async Task<List<Reservation>> GetByUserIdAsync(int userId)
        {
            return await _context.Reservations
                .Include(r => r.Event)
                .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                        .ThenInclude(s => s.PricingZone)
                .Include(r => r.Tickets)
                .Include(r => r.Payments)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère toutes les réservations pour un événement
        /// </summary>
        public async Task<List<Reservation>> GetByEventIdAsync(int eventId)
        {
            return await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                .Where(r => r.EventId == eventId)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les réservations expirées (Pending et ExpiresAt dépassé)
        /// </summary>
        public async Task<List<Reservation>> GetExpiredReservationsAsync()
        {
            return await _context.Reservations
                .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                .Where(r => r.Status == ReservationStatus.Pending
                         && r.ExpiresAt.HasValue
                         && r.ExpiresAt.Value < DateTime.UtcNow)
                .ToListAsync();
        }

        /// <summary>
        /// Crée une nouvelle réservation
        /// </summary>
        public async Task<Reservation> CreateAsync(Reservation reservation)
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        /// <summary>
        /// Met à jour une réservation existante
        /// </summary>
        public async Task UpdateAsync(Reservation reservation)
        {
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Supprime une réservation
        /// NOTE: Ne sauvegarde pas les changements - utilisez UnitOfWork.SaveChangesAsync()
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
                // Ne pas sauvegarder ici - laisser le UnitOfWork gérer la transaction
            }
        }
    }
}
