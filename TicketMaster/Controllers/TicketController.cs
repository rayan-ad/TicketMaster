using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketMaster.DTOs;
using TicketMaster.Services;

namespace TicketMaster.Controllers
{
    /// <summary>
    /// API controller for managing tickets.
    /// Requires authentication. Users can only access their own tickets.
    /// Admin/Organisateur roles can validate tickets at venue entrance.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        /// <summary>
        /// Retrieves all tickets belonging to the authenticated user.
        /// </summary>
        /// <returns>List of all tickets owned by the user.</returns>
        [HttpGet("my")]
        public async Task<ActionResult<List<TicketDto>>> GetMyTickets()
        {
            var userId = GetCurrentUserId();
            var tickets = await _ticketService.GetMyTicketsAsync(userId);
            return Ok(tickets);
        }

        /// <summary>
        /// Retrieves all tickets for a specific reservation.
        /// User can only access tickets from their own reservations.
        /// </summary>
        /// <param name="reservationId">The ID of the reservation.</param>
        /// <returns>List of tickets for the reservation.</returns>
        [HttpGet("reservation/{reservationId}")]
        public async Task<ActionResult<List<TicketDto>>> GetTicketsByReservation(int reservationId)
        {
            var userId = GetCurrentUserId();
            var tickets = await _ticketService.GetTicketsByReservationAsync(reservationId, userId);

            if (tickets == null || tickets.Count == 0)
            {
                return NotFound(new { message = "Aucun billet trouvé pour cette réservation." });
            }

            return Ok(tickets);
        }

        /// <summary>
        /// Retrieves a specific ticket by ID.
        /// User can only access their own tickets.
        /// </summary>
        /// <param name="id">The ID of the ticket.</param>
        /// <returns>The ticket details with QR code and validation status.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<TicketDto>> GetTicket(int id)
        {
            var userId = GetCurrentUserId();
            var ticket = await _ticketService.GetTicketByIdAsync(id, userId);

            if (ticket == null)
            {
                return NotFound(new { message = "Billet introuvable." });
            }

            return Ok(ticket);
        }

        /// <summary>
        /// Retrieves a ticket by its unique ticket number.
        /// Accessible without user context for QR code scanning.
        /// </summary>
        /// <param name="ticketNumber">The unique ticket number (format: TKT-{timestamp}-{seatId}).</param>
        /// <returns>The ticket details with QR code information.</returns>
        [HttpGet("number/{ticketNumber}")]
        public async Task<ActionResult<TicketDto>> GetTicketByNumber(string ticketNumber)
        {
            var ticket = await _ticketService.GetTicketByNumberAsync(ticketNumber);

            if (ticket == null)
            {
                return NotFound(new { message = "Billet introuvable." });
            }

            return Ok(ticket);
        }

        /// <summary>
        /// Validates a ticket by scanning its QR code at the venue entrance.
        /// Marks the ticket as used and records the validation timestamp.
        /// Requires Admin or Organisateur role.
        /// </summary>
        /// <param name="ticketNumber">The unique ticket number to validate.</param>
        /// <returns>Success or error message.</returns>
        [HttpPost("validate/{ticketNumber}")]
        [Authorize(Roles = "Admin,Organisateur")]
        public async Task<IActionResult> ValidateTicket(string ticketNumber)
        {
            var success = await _ticketService.ValidateTicketAsync(ticketNumber);

            if (!success)
            {
                return BadRequest(new { message = "Billet invalide ou déjà utilisé." });
            }

            return Ok(new { message = "Billet validé avec succès." });
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
