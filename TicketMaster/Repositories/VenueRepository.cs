using Microsoft.EntityFrameworkCore;
using TicketMaster.DataAccess;
using TicketMaster.Models;

namespace TicketMaster.Repositories
{
    public class VenueRepository : IVenueRepository
    {
        private readonly TicketMasterContext _db;
        public VenueRepository(TicketMasterContext db)
        {
            _db = db;
        }

        public Task<Venue?> GetByIdAsync(int id) =>
            _db.Venues
                .Include(v => v.PricingZones)
                .Include(v => v.Seats)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

        public Task<List<Venue>> ListAsync() =>
            _db.Venues
                .Include(v => v.PricingZones)
                .Include(v => v.Seats)
                .ToListAsync();

        public Task AddAsync(Venue v) { _db.Venues.Add(v); return Task.CompletedTask; }
        public void Update(Venue v) => _db.Venues.Update(v);
        public void Remove(Venue v) => _db.Venues.Remove(v);
    }
}
