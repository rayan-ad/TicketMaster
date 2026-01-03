using Microsoft.AspNetCore.Mvc;
using TicketMaster.Services;

namespace TicketMaster.Controllers
{
    /// <summary>
    /// API controller for managing seat availability and temporary holds.
    /// Provides endpoints for holding and releasing seats for events.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SeatsController : ControllerBase
    {
        private readonly ISeatService _seatService;

        public SeatsController(ISeatService seatService)
        {
            _seatService = seatService;
        }

        /// <summary>
        /// Temporarily reserves a seat for an event (hold).
        /// Called when a user selects a seat in the UI.
        /// The hold expires after the specified TTL if not confirmed.
        /// </summary>
        /// <param name="seatId">The ID of the seat to hold.</param>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="ttl">Time-to-live in minutes for the hold (default: 15).</param>
        /// <returns>Success or error message with expiration time.</returns>
        [HttpPut("{seatId}/hold")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HoldSeat(int seatId, [FromQuery] int eventId, [FromQuery] int ttl = 15)
        {
            try
            {
                var success = await _seatService.HoldSeatAsync(eventId, seatId, ttl);
                if (!success)
                {
                    return BadRequest(new { message = "Impossible de réserver ce siège. Il est peut-être déjà réservé ou vendu." });
                }

                return Ok(new { message = "Siège réservé temporairement.", seatId, eventId, expiresInMinutes = ttl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la réservation.", error = ex.Message });
            }
        }

        /// <summary>
        /// Releases a temporarily held seat for an event.
        /// Called when a user deselects a seat in the UI.
        /// </summary>
        /// <param name="seatId">The ID of the seat to release.</param>
        /// <param name="eventId">The ID of the event.</param>
        /// <returns>Success or error message.</returns>
        [HttpPut("{seatId}/release")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReleaseSeat(int seatId, [FromQuery] int eventId)
        {
            try
            {
                var success = await _seatService.ReleaseSeatAsync(eventId, seatId);
                if (!success)
                {
                    return BadRequest(new { message = "Impossible de libérer ce siège. Il n'est peut-être pas en réservation temporaire." });
                }

                return Ok(new { message = "Siège libéré.", seatId, eventId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la libération.", error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves all seats in a specific pricing zone for an event.
        /// Includes information about seat availability and pricing.
        /// </summary>
        /// <param name="pricingZoneId">The ID of the pricing zone.</param>
        /// <param name="eventId">The ID of the event.</param>
        /// <returns>List of seats in the pricing zone with their current states.</returns>
        [HttpGet("pricingZone/{pricingZoneId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSeatsByPricingZone(int pricingZoneId, [FromQuery] int eventId)
        {
            try
            {
                var seats = await _seatService.GetSeatsByPricingZoneAsync(eventId, pricingZoneId);
                return Ok(seats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Une erreur est survenue.", error = ex.Message });
            }
        }
    }
}
