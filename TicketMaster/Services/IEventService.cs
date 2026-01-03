using TicketMaster.DTOs;

namespace TicketMaster.Services
{
    /// <summary>
    /// Interface du Service Event
    ///
    /// RÔLE DU SERVICE :
    /// ==================
    /// Le Service contient la LOGIQUE MÉTIER de l'application.
    ///
    /// CE QU'IL FAIT :
    /// - Orchestrer plusieurs repositories (Event, Seat, Reservation, etc.)
    /// - Calculer des statistiques (taux de remplissage, revenus, etc.)
    /// - Mapper Entity vers DTO (et inversement)
    /// - Valider les règles métier (ex: "on ne peut pas supprimer un Event avec des réservations")
    /// - Appliquer la logique business complexe
    ///
    /// CE QU'IL NE FAIT PAS :
    /// - Requêtes SQL directes (c'est le job du Repository)
    /// - Gestion HTTP (codes 200, 404, etc. - c'est le job du Controller)
    ///
    /// IMPORTANT : Le Service retourne des DTOs, JAMAIS des entités directement !
    /// </summary>
    public interface IEventService
    {
        // GET - Liste d'events (version allégée)
        Task<List<EventListDto>> GetAllEventsAsync();

        // GET - Event détaillé avec toutes les stats calculées
        Task<EventDto?> GetEventByIdAsync(int id);

        // POST - Créer un nouvel event
        Task<EventDto> CreateEventAsync(CreateEventDto createDto);

        // PUT - Mettre à jour un event existant
        Task<EventDto?> UpdateEventAsync(UpdateEventDto updateDto);

        // DELETE - Supprimer un event (avec validation métier)
        Task<bool> DeleteEventAsync(int id);

        // Méthodes supplémentaires avec logique métier
        Task<List<EventListDto>> GetUpcomingEventsAsync();
        Task<List<EventListDto>> GetEventsByTypeAsync(string type);
        Task<List<EventListDto>> GetEventsByVenueAsync(int venueId);
    }
}
