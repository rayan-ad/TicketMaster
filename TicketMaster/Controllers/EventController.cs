using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketMaster.DTOs;
using TicketMaster.Services;

namespace TicketMaster.Controllers
{
    /// <summary>
    /// Controller Event - GESTION HTTP UNIQUEMENT
    ///
    /// RÔLE DU CONTROLLER :
    /// =====================
    /// Le Controller est responsable de la COUCHE HTTP.
    ///
    /// CE QU'IL FAIT :
    /// - Recevoir les requêtes HTTP
    /// - Valider le ModelState (grâce aux attributs sur les DTOs)
    /// - Appeler le Service
    /// - Retourner les bons codes HTTP (200, 201, 400, 404, 500, etc.)
    /// - Gérer les exceptions
    ///
    /// CE QU'IL NE FAIT PAS :
    /// - Logique métier (c'est le Service)
    /// - Calculs (c'est le Service)
    /// - Requêtes DB (c'est le Repository via le Service)
    /// - Mapping inline (c'est le Service qui retourne des DTOs)
    ///
    /// Le Controller est MINCE, il ne fait que du HTTP !
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ISeatService _seatService;

        public EventController(IEventService eventService, ISeatService seatService)
        {
            _eventService = eventService;
            _seatService = seatService;
        }

        // ============================================================
        // GET /api/event - Liste de tous les events
        // ============================================================

        /// <summary>
        /// Récupère la liste de tous les events avec pagination
        /// </summary>
        /// <param name="pageNumber">Numéro de page (défaut: 1)</param>
        /// <param name="pageSize">Taille de page (défaut: 9)</param>
        /// <returns>Résultat paginé d'events (version allégée)</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<EventListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllEvents(int pageNumber = 1, int pageSize = 9)
        {
            try
            {
                var events = await _eventService.GetAllEventsAsync();
                var totalCount = events.Count;
                var items = events.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                var result = new PaginatedResult<EventListDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log l'erreur ici (avec Serilog, NLog, etc.)
                return StatusCode(500, new { message = "Une erreur est survenue.", error = ex.Message });
            }
        }

        // ============================================================
        // GET /api/event/{id} - Event détaillé
        // ============================================================

        /// <summary>
        /// Récupère un event par son ID avec toutes les statistiques
        /// </summary>
        /// <param name="id">ID de l'event</param>
        /// <returns>Event détaillé avec stats</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEventById(int id)
        {
            try
            {
                var evt = await _eventService.GetEventByIdAsync(id);
                if (evt == null)
                {
                    return NotFound(new { message = $"Événement avec l'ID {id} introuvable." });
                }
                return Ok(evt);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Une erreur est survenue.", error = ex.Message });
            }
        }

        // ============================================================
        // POST /api/event - Créer un event
        // ============================================================

        /// <summary>
        /// Crée un nouvel event
        /// </summary>
        /// <param name="createDto">Données de l'event à créer</param>
        /// <returns>Event créé avec stats</returns>
        [HttpPost]
        [Authorize(Roles = "Admin,Organisateur")]
        [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto createDto)
        {
            // VALIDATION automatique grâce aux attributs [Required], [StringLength], etc. sur le DTO
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdEvent = await _eventService.CreateEventAsync(createDto);
                // 201 Created avec l'URL de la ressource créée
                return CreatedAtAction(
                    nameof(GetEventById),
                    new { id = createdEvent.Id },
                    createdEvent
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la création.", error = ex.Message });
            }
        }

        // ============================================================
        // PUT /api/event/{id} - Mettre à jour un event
        // ============================================================

        /// <summary>
        /// Met à jour un event existant
        /// </summary>
        /// <param name="id">ID de l'event à modifier</param>
        /// <param name="updateDto">Nouvelles données de l'event</param>
        /// <returns>Event mis à jour avec stats</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Organisateur")]
        [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto updateDto)
        {
            // Vérifier que l'ID dans l'URL correspond à l'ID dans le body
            if (id != updateDto.Id)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le body." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedEvent = await _eventService.UpdateEventAsync(updateDto);
                if (updatedEvent == null)
                {
                    return NotFound(new { message = $"Événement avec l'ID {id} introuvable." });
                }
                return Ok(updatedEvent);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour.", error = ex.Message });
            }
        }

        // ============================================================
        // DELETE /api/event/{id} - Supprimer un event
        // ============================================================

        /// <summary>
        /// Supprime un event
        /// RÈGLE MÉTIER (gérée par le Service) : On ne peut pas supprimer un event avec des réservations
        /// </summary>
        /// <param name="id">ID de l'event à supprimer</param>
        /// <returns>Message de confirmation</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                var result = await _eventService.DeleteEventAsync(id);
                if (!result)
                {
                    return NotFound(new { message = $"Événement avec l'ID {id} introuvable." });
                }
                return Ok(new { message = $"Événement {id} supprimé avec succès." });
            }
            catch (InvalidOperationException ex)
            {
                // GESTION DES RÈGLES MÉTIER :
                // Le Service lance une InvalidOperationException si l'event a des réservations
                // Le Controller la catch et retourne un 400 Bad Request
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la suppression.", error = ex.Message });
            }
        }

        // ============================================================
        // ROUTES SUPPLÉMENTAIRES
        // ============================================================

        /// <summary>
        /// Récupère les events à venir
        /// </summary>
        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(List<EventListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingEvents()
        {
            try
            {
                var events = await _eventService.GetUpcomingEventsAsync();
                return Ok(events);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Une erreur est survenue.", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les events par type (Sport, Concert, etc.)
        /// </summary>
        [HttpGet("type/{type}")]
        [ProducesResponseType(typeof(List<EventListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEventsByType(string type)
        {
            try
            {
                var events = await _eventService.GetEventsByTypeAsync(type);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Une erreur est survenue.", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les events d'un venue spécifique
        /// </summary>
        [HttpGet("venue/{venueId}")]
        [ProducesResponseType(typeof(List<EventListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEventsByVenue(int venueId)
        {
            try
            {
                var events = await _eventService.GetEventsByVenueAsync(venueId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Une erreur est survenue.", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les sièges d'un event spécifique
        /// Route utilisée par le frontend pour afficher la carte interactive du stade
        /// </summary>
        [HttpGet("{id}/seats")]
        [ProducesResponseType(typeof(List<SeatDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSeatsForEvent(int id)
        {
            try
            {
                var seats = await _seatService.GetSeatsForEventAsync(id);
                if (seats == null || seats.Count == 0)
                {
                    return NotFound(new { message = "Event ou venue introuvable." });
                }

                // Le Service retourne déjà des SeatDto propres !
                // Mais pour garder la compatibilité avec le frontend Angular,
                // on retourne dans le format attendu (avec "zone" comme objet)
                return Ok(seats.Select(s => new
                {
                    id = s.Id,
                    row = s.Row,
                    number = s.Number,
                    state = s.State,
                    price = s.Price,
                    zone = new
                    {
                        id = s.PricingZoneId,
                        name = s.ZoneName,
                        color = s.ZoneColor
                    }
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Une erreur est survenue.", error = ex.Message });
            }
        }
    }
}
