using TicketMaster.Enum;
using TicketMaster.Models;

namespace TicketMaster.Repositories
{
    /// <summary>
    /// Repository pour les états de sièges par event
    /// </summary>
    public interface ISeatReservationStateRepository
    {
        /// <summary>
        /// Récupère tous les états de sièges pour un event spécifique
        /// </summary>
        Task<List<SeatReservationState>> GetByEventIdAsync(int eventId);

        /// <summary>
        /// Récupère un état spécifique (Event + Seat)
        /// </summary>
        Task<SeatReservationState?> GetByEventAndSeatAsync(int eventId, int seatId);

        /// <summary>
        /// Met à jour un état
        /// </summary>
        Task UpdateAsync(SeatReservationState state);

        /// <summary>
        /// Crée un nouvel état
        /// </summary>
        Task AddAsync(SeatReservationState state);
    }
}
