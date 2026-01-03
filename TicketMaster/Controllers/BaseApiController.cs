using Microsoft.AspNetCore.Mvc;
using TicketMaster.Common;

namespace TicketMaster.Controllers
{
    /// <summary>
    /// Contrôleur de base pour tous les contrôleurs API.
    /// Fournit des méthodes utilitaires pour gérer les réponses HTTP standardisées.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Retourne une réponse 200 OK avec des données
        /// </summary>
        protected IActionResult OkResponse<T>(T data)
        {
            return Ok(data);
        }

        /// <summary>
        /// Retourne une réponse 201 Created avec des données
        /// </summary>
        protected IActionResult CreatedResponse<T>(string actionName, object routeValues, T data)
        {
            return CreatedAtAction(actionName, routeValues, data);
        }

        /// <summary>
        /// Retourne une réponse 400 Bad Request avec un message d'erreur
        /// </summary>
        protected IActionResult BadRequestResponse(string message)
        {
            return BadRequest(new { message });
        }

        /// <summary>
        /// Retourne une réponse 404 Not Found avec un message d'erreur
        /// </summary>
        protected IActionResult NotFoundResponse(string message)
        {
            return NotFound(new { message });
        }

        /// <summary>
        /// Retourne une réponse 500 Internal Server Error avec un message d'erreur
        /// </summary>
        protected IActionResult InternalServerErrorResponse(string message, string errorDetails = "")
        {
            var response = new { message, error = errorDetails };
            return StatusCode(500, response);
        }

        /// <summary>
        /// Retourne une réponse 500 Internal Server Error avec un message générique
        /// </summary>
        protected IActionResult InternalServerErrorResponse(Exception ex)
        {
            return InternalServerErrorResponse(AppConstants.ErrorMessages.GenericError, ex.Message);
        }

        /// <summary>
        /// Retourne une réponse 401 Unauthorized avec un message d'erreur
        /// </summary>
        protected IActionResult UnauthorizedResponse(string message)
        {
            return Unauthorized(new { message });
        }

        /// <summary>
        /// Retourne une réponse de succès avec un message personnalisé
        /// </summary>
        protected IActionResult SuccessResponse(string message)
        {
            return Ok(new { message });
        }
    }
}
