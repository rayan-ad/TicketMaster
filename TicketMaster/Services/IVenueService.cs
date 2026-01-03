using TicketMaster.DTOs;
using TicketMaster.Models;

namespace TicketMaster.Services
{
    public interface IVenueService
    {
        Task<Venue> CreateVenueAsync(Venue newVenue);
        Task<Venue> CreateVenueWithSeatsAsync(CreateVenueDto createDto);
        Task<Venue?> GetVenueByIdAsync(int id);
        Task<List<Venue>> GetAllVenuesAsync();
        Task<Venue?> UpdateVenueAsync(Venue updatedVenue);
        Task<bool> DeleteVenueAsync(int id);
    }
}
