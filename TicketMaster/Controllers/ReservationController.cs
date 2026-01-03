using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketMaster.DTOs;
using TicketMaster.Services;

namespace TicketMaster.Controllers
{
    /// <summary>
    /// API controller for managing reservations.
    /// Requires authentication. Users can only access their own reservations.
    /// Provides endpoints for creating, retrieving, and canceling reservations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        /// <summary>
        /// Retrieves all reservations of the authenticated user with pagination.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (default: 1).</param>
        /// <param name="pageSize">Number of reservations per page (default: 10).</param>
        /// <returns>Paginated list of the user's reservations.</returns>
        [HttpGet("my")]
        public async Task<ActionResult<PaginatedResult<ReservationDto>>> GetMyReservations(int pageNumber = 1, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var allReservations = await _reservationService.GetMyReservationsAsync(userId);

            var totalCount = allReservations.Count;
            var items = allReservations.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            var result = new PaginatedResult<ReservationDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
        }

        /// <summary>
        /// Retrieves a specific reservation by ID.
        /// User can only access their own reservations.
        /// </summary>
        /// <param name="id">The ID of the reservation to retrieve.</param>
        /// <returns>The reservation details if it belongs to the authenticated user.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationDto>> GetReservation(int id)
        {
            var userId = GetCurrentUserId();
            var reservation = await _reservationService.GetByIdAsync(id, userId);

            if (reservation == null)
            {
                return NotFound(new { message = "Réservation introuvable." });
            }

            return Ok(reservation);
        }

        /// <summary>
        /// Creates a new reservation for the authenticated user.
        /// Reserves selected seats temporarily (with TTL).
        /// </summary>
        /// <param name="dto">DTO containing event ID and selected seat IDs.</param>
        /// <returns>The created reservation with status and expiration time.</returns>
        [HttpPost]
        public async Task<ActionResult<ReservationDto>> CreateReservation([FromBody] CreateReservationDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var reservation = await _reservationService.CreateReservationAsync(dto, userId);
                return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, reservation);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cancels a pending reservation.
        /// Only reservations with Pending status can be canceled.
        /// </summary>
        /// <param name="id">The ID of the reservation to cancel.</param>
        /// <returns>Success or error message.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var userId = GetCurrentUserId();
            var success = await _reservationService.CancelReservationAsync(id, userId);

            if (!success)
            {
                return BadRequest(new { message = "Impossible d'annuler cette réservation." });
            }

            return Ok(new { message = "Réservation annulée avec succès." });
        }

        /// <summary>
        /// Extracts the user ID from the JWT token claims.
        /// </summary>
        /// <returns>The authenticated user's ID.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user is not authenticated.</exception>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Utilisateur non authentifié.");
            }
            return userId;
        }
    }
}
