using TicketMaster.DTOs;

namespace TicketMaster.Services
{
    /// <summary>
    /// Service pour gérer les sièges
    /// LOGIQUE MÉTIER :
    /// - Récupérer les sièges avec leurs infos de zone tarifaire
    /// - Mapper vers SeatDto
    /// - Gérer la réservation temporaire (hold/release)
    /// </summary>
    public interface ISeatService
    {
        /// <summary>
        /// Récupère tous les sièges d'un event avec leurs infos complètes
        /// </summary>
        Task<List<SeatDto>> GetSeatsForEventAsync(int eventId);

        /// <summary>
        /// Récupère les sièges d'une zone tarifaire spécifique pour un event
        /// </summary>
        Task<List<SeatDto>> GetSeatsByPricingZoneAsync(int eventId, int pricingZoneId);

        /// <summary>
        /// Réserve temporairement un siège pour un event spécifique (state = ReservedTemp)
        /// </summary>
        Task<bool> HoldSeatAsync(int eventId, int seatId, int ttlMinutes = 15);

        /// <summary>
        /// Libère un siège réservé temporairement pour un event (state = Free)
        /// </summary>
        Task<bool> ReleaseSeatAsync(int eventId, int seatId);
    }
}
