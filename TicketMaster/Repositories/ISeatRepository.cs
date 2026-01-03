
using TicketMaster.Enum;
using TicketMaster.Models;

namespace TicketMaster.Repositories
{
    public interface ISeatRepository
    {
        // Define methods for seat data access here
        /*Task<List<Seat>> GetByVenueIdAsync(int venueId, CancellationToken ct);          // si tu listes par salle
        Task<List<Seat>> GetByPricingZoneIdAsync(int pricingZoneId, CancellationToken ct);
        Task<Seat?> GetByIdAsync(int seatId, CancellationToken ct);
        Task AddAsync(Seat seat, CancellationToken ct);                                  // utile pour seed/admin
        Task UpdateAsync(Seat seat, CancellationToken ct);
        */
        Task<List<Seat>> GetByVenueIdAsync(int venueId);
        Task<List<Seat>> GetByPricingZoneIdAsync(int pricingZoneId);
        Task<Seat?> GetByIdAsync(int seatId);
        Task UpdateAsync(Seat seat);
    }
}
