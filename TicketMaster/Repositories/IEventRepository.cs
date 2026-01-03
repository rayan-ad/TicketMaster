using TicketMaster.Models;

namespace TicketMaster.Repositories
{
    /// <summary>
    /// Interface du Repository Event
    ///
    /// RÔLE DU REPOSITORY :
    /// =====================
    /// Le Repository est UNIQUEMENT responsable de l'ACCÈS AUX DONNÉES.
    ///
    /// CE QU'IL FAIT :
    /// - Requêtes SQL/LINQ vers la base de données
    /// - Include() pour charger les relations (Venue, etc.)
    /// - Méthodes de recherche spécifiques (GetByVenueId, etc.)
    ///
    /// CE QU'IL NE FAIT PAS :
    /// - Calculs métier (taux de remplissage, revenus, etc.)
    /// - Validations métier (vérifier si on peut supprimer, etc.)
    /// - Mapping Entity -> DTO
    /// - Orchestration de plusieurs repositories
    ///
    /// Tout ça, c'est le JOB du SERVICE !
    /// </summary>
    public interface IEventRepository
    {
        // CRUD de base
        Task<Event?> GetByIdAsync(int id);
        Task<List<Event>> ListAsync();
        Task AddAsync(Event e);
        void Update(Event e);
        void Remove(Event e);

        // Méthodes avec Include() pour charger les relations
        Task<Event?> GetByIdWithVenueAsync(int id);
        Task<List<Event>> ListWithVenuesAsync();

        // Méthodes de recherche spécifiques (accès DB uniquement)
        Task<List<Event>> GetEventsByVenueIdAsync(int venueId);
        Task<List<Event>> GetUpcomingEventsAsync();
        Task<List<Event>> GetEventsByTypeAsync(string type);
        Task<bool> HasReservationsAsync(int eventId);
    }
}
