using Microsoft.EntityFrameworkCore;
using TicketMaster.DataAccess;
using TicketMaster.Models;

namespace TicketMaster.Repositories
{
    public class SeatReservationStateRepository : ISeatReservationStateRepository
    {
        private readonly TicketMasterContext _db;

        public SeatReservationStateRepository(TicketMasterContext db)
        {
            _db = db;
        }

        public Task<List<SeatReservationState>> GetByEventIdAsync(int eventId) =>
            _db.SeatReservationStates
                .AsNoTracking()
                .Include(srs => srs.Seat)
                    .ThenInclude(s => s.PricingZone)
                .Where(srs => srs.EventId == eventId)
                .ToListAsync();

        public Task<SeatReservationState?> GetByEventAndSeatAsync(int eventId, int seatId) =>
            _db.SeatReservationStates
                .Include(srs => srs.Seat)
                    .ThenInclude(s => s.PricingZone)
                .FirstOrDefaultAsync(srs => srs.EventId == eventId && srs.SeatId == seatId);

        public Task UpdateAsync(SeatReservationState state)
        {
            state.UpdatedAt = DateTime.UtcNow;
            _db.SeatReservationStates.Update(state);
            return Task.CompletedTask;
        }

        public Task AddAsync(SeatReservationState state)
        {
            _db.SeatReservationStates.Add(state);
            return Task.CompletedTask;
        }
    }
}
