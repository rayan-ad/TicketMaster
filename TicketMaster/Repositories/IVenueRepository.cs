using TicketMaster.Models;

namespace TicketMaster.Repositories
{
    public interface IVenueRepository
    {
        Task<Venue?> GetByIdAsync(int id);
        Task<List<Venue>> ListAsync();
        Task AddAsync(Venue v);
        void Update(Venue v);
        void Remove(Venue v);
    }
}
