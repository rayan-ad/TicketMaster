using Microsoft.EntityFrameworkCore;
using TicketMaster.DataAccess;
using TicketMaster.Models;

namespace TicketMaster.Repositories
{
    public class SeatRepository : ISeatRepository
    {
        private readonly TicketMasterContext _db;

        public SeatRepository(TicketMasterContext db) => _db = db;

        public Task<List<Seat>> GetByVenueIdAsync(int venueId) =>
            _db.Seats.Include(s => s.PricingZone)
                     .Where(s => s.PricingZone.VenueId == venueId) // <-- suppose PricingZone.VenueId
                     .ToListAsync();

        public Task<List<Seat>> GetByPricingZoneIdAsync(int pricingZoneId) =>
            _db.Seats.Include(s => s.PricingZone)
                     .Where(s => s.PricingZoneId == pricingZoneId)
                     .ToListAsync();

        public Task<Seat?> GetByIdAsync(int seatId) =>
            _db.Seats.Include(s => s.PricingZone)
                     .FirstOrDefaultAsync(s => s.Id == seatId);

        public Task UpdateAsync(Seat seat)
        {
            _db.Seats.Update(seat);
            return Task.CompletedTask;
        }
    }
}
