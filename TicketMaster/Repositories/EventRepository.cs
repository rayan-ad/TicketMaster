using Microsoft.EntityFrameworkCore;
using TicketMaster.DataAccess;
using TicketMaster.Models;

namespace TicketMaster.Repositories
{
    /// <summary>
    /// Repository Event - ACCÈS AUX DONNÉES UNIQUEMENT
    ///
    /// RAPPEL : Le Repository ne fait QUE des requêtes DB.
    /// Pas de logique métier, pas de calculs, pas de mapping !
    /// </summary>
    public class EventRepository : IEventRepository
    {
        private readonly TicketMasterContext _db;

        public EventRepository(TicketMasterContext db)
        {
            _db = db;
        }

        // ============================================================
        // CRUD DE BASE
        // ============================================================

        /// <summary>
        /// Récupère un Event par Id (SANS les relations)
        /// AsNoTracking() = lecture seule, meilleures performances
        /// </summary>
        public Task<Event?> GetByIdAsync(int id) =>
            _db.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

        /// <summary>
        /// Récupère tous les Events (SANS les relations)
        /// </summary>
        public Task<List<Event>> ListAsync() =>
            _db.Events
                .AsNoTracking()
                .ToListAsync();

        /// <summary>
        /// Ajoute un Event (le SaveChanges sera fait par le UnitOfWork)
        /// </summary>
        public Task AddAsync(Event e)
        {
            _db.Events.Add(e);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Met à jour un Event
        /// </summary>
        public void Update(Event e) => _db.Events.Update(e);

        /// <summary>
        /// Supprime un Event
        /// </summary>
        public void Remove(Event e) => _db.Events.Remove(e);

        // ============================================================
        // MÉTHODES AVEC RELATIONS (Include)
        // ============================================================

        /// <summary>
        /// Récupère un Event AVEC son Venue
        /// POURQUOI ? Le Service en a besoin pour calculer les stats
        /// </summary>
        public Task<Event?> GetByIdWithVenueAsync(int id) =>
            _db.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.Id == id);

        /// <summary>
        /// Récupère tous les Events AVEC leurs Venues
        /// Utile pour la liste des events avec infos du lieu
        /// </summary>
        public Task<List<Event>> ListWithVenuesAsync() =>
            _db.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .OrderBy(e => e.Date)
                .ToListAsync();

        // ============================================================
        // MÉTHODES DE RECHERCHE SPÉCIFIQUES
        // ============================================================

        /// <summary>
        /// Récupère tous les Events d'un Venue spécifique
        /// </summary>
        public Task<List<Event>> GetEventsByVenueIdAsync(int venueId) =>
            _db.Events
                .AsNoTracking()
                .Where(e => e.VenueId == venueId)
                .OrderBy(e => e.Date)
                .ToListAsync();

        /// <summary>
        /// Récupère les Events à venir (date >= aujourd'hui)
        /// </summary>
        public Task<List<Event>> GetUpcomingEventsAsync() =>
            _db.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .Where(e => e.Date >= DateTime.Now)
                .OrderBy(e => e.Date)
                .ToListAsync();

        /// <summary>
        /// Récupère les Events par type (Sport, Concert, etc.)
        /// </summary>
        public Task<List<Event>> GetEventsByTypeAsync(string type) =>
            _db.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .Where(e => e.Type == type)
                .OrderBy(e => e.Date)
                .ToListAsync();

        /// <summary>
        /// Vérifie si un Event a des réservations
        /// POURQUOI ? Le Service en a besoin pour savoir si on peut supprimer l'Event
        /// </summary>
        public Task<bool> HasReservationsAsync(int eventId) =>
            _db.Reservations.AnyAsync(r => r.EventId == eventId);
    }
}
