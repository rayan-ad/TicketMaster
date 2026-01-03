using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketMaster.DTOs;
using TicketMaster.Models;
using TicketMaster.Services;

namespace TicketMaster.Controllers
{
    /// <summary>
    /// API controller for managing venues.
    /// Provides endpoints for creating, retrieving, updating, and deleting venues.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class VenueController : ControllerBase
    {
        private readonly IVenueService _venueService;

        public VenueController(IVenueService venueService)
        {
            _venueService = venueService;
        }

        /// <summary>
        /// Retrieves a paginated list of all venues.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1).</param>
        /// <param name="pageSize">Number of venues per page (default: 9).</param>
        /// <returns>A paginated list of venues with their pricing zones and seats.</returns>
        [HttpGet("list")]
        public async Task<IActionResult> List(int pageNumber = 1, int pageSize = 9)
        {
            try
            {
                var venues = await Task.Run(() => _venueService.GetAllVenuesAsync());

                // Projeter vers un format sans références circulaires
                var allVenues = venues.Select(v => new
                {
                    v.Id,
                    v.Name,
                    v.Capacity,
                    pricingZones = v.PricingZones?.Select(pz => new
                    {
                        pz.Id,
                        pz.Name,
                        pz.Price,
                        pz.Color
                    }).ToList(),
                    seats = v.Seats?.Select(s => new
                    {
                        s.Id,
                        s.Row,
                        s.Number,
                        pricingZoneId = s.PricingZoneId
                    }).ToList()
                }).ToList();

                var totalCount = allVenues.Count;
                var items = allVenues.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var result = new PaginatedResult<object>
                {
                    Items = items.Cast<object>().ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, new { message = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        /// <summary>
        /// Retrieves a specific venue by its ID.
        /// </summary>
        /// <param name="id">The ID of the venue to retrieve.</param>
        /// <returns>The venue with its pricing zones and seats.</returns>
        [HttpGet("get/{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var venue = await Task.Run(() => _venueService.GetVenueByIdAsync(id));
                if (venue == null)
                {
                    return NotFound(new { message = $"Venue with id {id} not found." });
                }

                // Projeter vers un format sans références circulaires
                var result = new
                {
                    venue.Id,
                    venue.Name,
                    venue.Capacity,
                    pricingZones = venue.PricingZones?.Select(pz => new
                    {
                        pz.Id,
                        pz.Name,
                        pz.Price,
                        pz.Color
                    }).ToList(),
                    seats = venue.Seats?.Select(s => new
                    {
                        s.Id,
                        s.Row,
                        s.Number,
                        pricingZoneId = s.PricingZoneId
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, new { message = ex.Message });
            }
        }


        /// <summary>
        /// Creates a new venue with automatically generated seats.
        /// Requires Admin or Organisateur role.
        /// </summary>
        /// <param name="createDto">DTO containing venue and pricing zone information.</param>
        /// <returns>The created venue with generated seats and pricing zones.</returns>
        [Route("createVenue")]
        [HttpPost]
        [Authorize(Roles = "Admin,Organisateur")]
        public async Task<IActionResult> Create([FromBody] CreateVenueDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var venue = await Task.Run(() => _venueService.CreateVenueWithSeatsAsync(createDto));
                if (venue == null) return BadRequest(new { message = "Venue could not be created." });

                // Retourner directement la venue créée avec le nombre de sièges générés
                return Ok(new
                {
                    message = $"Venue created successfully with {venue.Seats.Count} seats!",
                    venue = new
                    {
                        venue.Id,
                        venue.Name,
                        venue.Capacity,
                        seatsGenerated = venue.Seats.Count,
                        pricingZones = venue.PricingZones.Count
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing venue.
        /// Requires Admin or Organisateur role.
        /// </summary>
        /// <param name="updatedVenue">The venue with updated information.</param>
        /// <returns>The updated venue.</returns>
        [Route("updateVenue")]
        [HttpPut]
        [Authorize(Roles = "Admin,Organisateur")]
        public async Task<IActionResult> Update([FromBody] Venue updatedVenue)
        {
            try
            {
                var venue = await Task.Run(() => _venueService.UpdateVenueAsync(updatedVenue));
                if (venue == null) return NotFound($"Venue with id {updatedVenue.Id} not found.");
                return Ok(venue);
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Deletes a venue by its ID.
        /// Requires Admin role. Venue must not have associated events.
        /// </summary>
        /// <param name="id">The ID of the venue to delete.</param>
        /// <returns>Success or error message.</returns>
        [Route("deleteVenue/{id}")]
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await Task.Run(() => _venueService.DeleteVenueAsync(id));
                if (!result) return NotFound(new { message = $"Venue with id {id} not found." });
                return Ok(new { message = $"Venue with id {id} deleted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                // Erreur métier: venue a des événements liés
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return StatusCode((int)System.Net.HttpStatusCode.InternalServerError, new { message = ex.Message });
            }
        }
    }
}
