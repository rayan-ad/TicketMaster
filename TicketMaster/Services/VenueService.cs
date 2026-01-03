using Microsoft.EntityFrameworkCore;
using TicketMaster.DataAccess;
using TicketMaster.DTOs;
using TicketMaster.Models;
using TicketMaster.Repositories;

namespace TicketMaster.Services
{
    /// <summary>
    /// Service for managing venues and their associated seats and pricing zones.
    /// Provides functionality to create, retrieve, update, and delete venues.
    /// </summary>
    public class VenueService : IVenueService
    {
        private readonly IVenueRepository _venueRepository;
        private readonly IUnitOfWork _uow;
        private readonly TicketMasterContext _context;

        public VenueService(IVenueRepository venueRepository, IUnitOfWork uow, TicketMasterContext context)
        {
            _venueRepository = venueRepository;
            _uow = uow;
            _context = context;
        }

        /// <summary>
        /// Creates a new venue with the specified details.
        /// </summary>
        /// <param name="newVenue">The venue object to create.</param>
        /// <returns>The created venue with its assigned ID.</returns>
        public async Task<Venue> CreateVenueAsync(Venue newVenue)
        {
            await _venueRepository.AddAsync(newVenue);
            await _uow.SaveChangesAsync();
            return newVenue;
        }

        /// <summary>
        /// Creates a new venue with automatically generated seats.
        /// Generates seats in rows of 10 based on the seat count specified for each pricing zone.
        /// Validates that the total seat count does not exceed the venue capacity.
        /// </summary>
        /// <param name="createDto">DTO containing venue information and pricing zone details with seat counts.</param>
        /// <returns>The created venue with all generated seats and pricing zones.</returns>
        /// <exception cref="ArgumentException">Thrown when total seats exceed venue capacity.</exception>
        public async Task<Venue> CreateVenueWithSeatsAsync(CreateVenueDto createDto)
        {
            // VALIDATION: Vérifier que le nombre total de sièges ne dépasse pas la capacité
            int totalSeats = createDto.PricingZones.Sum(z => z.SeatCount);
            if (totalSeats > createDto.Capacity)
            {
                throw new ArgumentException($"Le nombre total de sièges ({totalSeats}) dépasse la capacité du venue ({createDto.Capacity}).");
            }

            // Créer le venue
            var venue = new Venue
            {
                Name = createDto.Name,
                Capacity = createDto.Capacity,
                PricingZones = new List<PricingZone>(),
                Seats = new List<Seat>()
            };

            // Créer les zones tarifaires
            foreach (var zoneDto in createDto.PricingZones)
            {
                var pricingZone = new PricingZone
                {
                    Name = zoneDto.Name,
                    Price = zoneDto.Price,
                    Color = zoneDto.Color,
                    Venue = venue
                };
                venue.PricingZones.Add(pricingZone);
            }

            // Générer automatiquement les sièges selon le nombre spécifié par zone
            for (int zoneIndex = 0; zoneIndex < venue.PricingZones.Count; zoneIndex++)
            {
                var pricingZone = venue.PricingZones.ElementAt(zoneIndex);
                var zoneDto = createDto.PricingZones[zoneIndex];
                int seatsForThisZone = zoneDto.SeatCount;

                // Générer les sièges en rangées de 10
                int seatsCreated = 0;
                int row = 1;

                while (seatsCreated < seatsForThisZone)
                {
                    int seatsInThisRow = Math.Min(10, seatsForThisZone - seatsCreated);

                    for (int seatNum = 1; seatNum <= seatsInThisRow; seatNum++)
                    {
                        var seat = new Seat
                        {
                            Row = $"{pricingZone.Name[0]}{row}",
                            Number = seatNum,
                            PricingZone = pricingZone
                        };
                        venue.Seats.Add(seat);
                        seatsCreated++;
                    }

                    row++;
                }
            }

            // Sauvegarder tout
            await _venueRepository.AddAsync(venue);
            await _uow.SaveChangesAsync();

            return venue;
        }

        /// <summary>
        /// Retrieves all venues from the database.
        /// </summary>
        /// <returns>A list of all venues.</returns>
        public async Task<List<Venue>> GetAllVenuesAsync()
        {
            return await _venueRepository.ListAsync();
        }

        /// <summary>
        /// Retrieves a specific venue by its ID.
        /// </summary>
        /// <param name="id">The ID of the venue to retrieve.</param>
        /// <returns>The venue if found; otherwise, null.</returns>
        public async Task<Venue?> GetVenueByIdAsync(int id)
        {
            return await _venueRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Updates an existing venue with new information.
        /// </summary>
        /// <param name="updatedVenue">The venue object with updated information.</param>
        /// <returns>The updated venue if found; otherwise, null.</returns>
        public async Task<Venue?> UpdateVenueAsync(Venue updatedVenue)
        {
            var existing = await GetVenueByIdAsync(updatedVenue.Id);
            if (existing == null) return null;

            existing.Name = updatedVenue.Name;
            existing.Capacity = updatedVenue.Capacity;
            existing.Seats = updatedVenue.Seats;
            existing.PricingZones = updatedVenue.PricingZones;

            _venueRepository.Update(existing);
            await _uow.SaveChangesAsync();

            return existing;
        }

        /// <summary>
        /// Deletes a venue if it has no associated events.
        /// </summary>
        /// <param name="id">The ID of the venue to delete.</param>
        /// <returns>True if the venue was deleted; false if the venue was not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the venue has associated events that must be deleted first.</exception>
        public async Task<bool> DeleteVenueAsync(int id)
        {
            var venue = await GetVenueByIdAsync(id);
            if (venue == null) return false;

            // PROTECTION: Vérifier s'il y a des événements liés à ce venue
            var hasEvents = await _context.Events.AnyAsync(e => e.VenueId == id);
            if (hasEvents)
            {
                throw new InvalidOperationException(
                    $"Impossible de supprimer la salle '{venue.Name}' car elle a des événements associés. " +
                    "Supprimez d'abord tous les événements de cette salle."
                );
            }

            _venueRepository.Remove(venue);
            await _uow.SaveChangesAsync();

            return true;
        }
    }
}
