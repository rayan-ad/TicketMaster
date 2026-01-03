using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketMaster.DTOs;
using TicketMaster.Services;

namespace TicketMaster.Controllers
{
    /// <summary>
    /// API controller for managing payments.
    /// Requires authentication. Users can only process payments for their own reservations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Processes payment for a pending reservation.
        /// Confirms the reservation and generates tickets upon successful payment.
        /// </summary>
        /// <param name="dto">DTO containing reservation ID and payment method.</param>
        /// <returns>The updated reservation with payment confirmation and generated tickets.</returns>
        [HttpPost("process")]
        public async Task<ActionResult<ReservationDto>> ProcessPayment([FromBody] ProcessPaymentDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var reservation = await _paymentService.ProcessPaymentAsync(dto, userId);
                return Ok(reservation);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
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
