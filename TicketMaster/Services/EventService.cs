using TicketMaster.DTOs;
using TicketMaster.Enum;
using TicketMaster.Models;
using TicketMaster.Repositories;

namespace TicketMaster.Services
{
    /// <summary>
    /// Service Event - LOGIQUE MÉTIER
    ///
    /// DIFFÉRENCE avec Repository :
    /// ==============================
    /// Repository :  SELECT * FROM Events WHERE Id = 1  (accès DB brut)
    /// Service    :  1. Appeler EventRepository.GetByIdWithVenueAsync()
    ///               2. Appeler SeatRepository.GetByVenueIdAsync()
    ///               3. CALCULER les stats (taux remplissage, revenus, etc.)
    ///               4. MAPPER vers EventDto
    ///               5. Retourner le DTO au Controller
    ///
    /// C'est ici que se trouve la VRAIE VALEUR AJOUTÉE !
    /// </summary>
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly ISeatReservationStateRepository _seatStateRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IUnitOfWork _uow;

        public EventService(
            IEventRepository eventRepository,
            ISeatReservationStateRepository seatStateRepository,
            ISeatRepository seatRepository,
            IUnitOfWork uow)
        {
            _eventRepository = eventRepository;
            _seatStateRepository = seatStateRepository;  // IMPORTANT : Le Service peut utiliser plusieurs repositories !
            _seatRepository = seatRepository;
            _uow = uow;
        }

        // ============================================================
        // GET - LISTE D'EVENTS (version allégée)
        // ============================================================

        /// <summary>
        /// Récupère tous les events avec stats de base
        /// LOGIQUE MÉTIER :
        /// - Récupérer les events avec leurs venues (via Repository)
        /// - Pour chaque event, calculer les stats de sièges via SeatReservationState
        /// - Mapper vers EventListDto (version allégée)
        /// </summary>
        public async Task<List<EventListDto>> GetAllEventsAsync()
        {
            // 1. RÉCUPÉRATION DES DONNÉES (via Repository)
            var events = await _eventRepository.ListWithVenuesAsync();

            // 2. CALCULS MÉTIER + MAPPING vers DTO
            var result = new List<EventListDto>();
            foreach (var evt in events)
            {
                // Récupérer les états de sièges pour cet event
                var seatStates = await _seatStateRepository.GetByEventIdAsync(evt.Id);

                // CALCULS MÉTIER
                var totalSeats = seatStates.Count;
                var availableSeats = seatStates.Count(ss => ss.State == SeatStatus.Free);
                var fillRate = totalSeats > 0
                    ? Math.Round((decimal)(totalSeats - availableSeats) / totalSeats * 100, 2)
                    : 0;

                // MAPPING Entity -> DTO
                result.Add(new EventListDto
                {
                    Id = evt.Id,
                    Name = evt.Name,
                    Date = evt.Date,
                    Type = evt.Type,
                    ImageEvent = evt.ImageEvent,
                    VenueName = evt.Venue?.Name ?? "N/A",
                    AvailableSeats = availableSeats,
                    FillRate = fillRate
                });
            }

            return result;
        }

        // ============================================================
        // GET - EVENT DÉTAILLÉ (avec TOUTES les stats)
        // ============================================================

        /// <summary>
        /// Récupère un event avec TOUTES les statistiques calculées
        /// LOGIQUE MÉTIER :
        /// - Récupérer l'event avec son venue
        /// - Récupérer tous les états de sièges pour cet event
        /// - CALCULER : total, disponibles, réservés, payés, taux remplissage, revenus
        /// - Mapper vers EventDto complet
        ///
        /// C'EST ICI LA DIFFÉRENCE AVEC LE REPOSITORY :
        /// Le Repository retourne juste l'Event brut de la DB.
        /// Le Service ENRICHIT avec des calculs métier !
        /// </summary>
        public async Task<EventDto?> GetEventByIdAsync(int id)
        {
            // 1. RÉCUPÉRATION DES DONNÉES (via Repository)
            var evt = await _eventRepository.GetByIdWithVenueAsync(id);
            if (evt == null) return null;

            // 2. RÉCUPÉRATION DES ÉTATS DE SIÈGES pour cet event
            var seatStates = await _seatStateRepository.GetByEventIdAsync(id);

            // 3. CALCULS MÉTIER (la vraie valeur ajoutée du Service !)
            var totalSeats = seatStates.Count;
            var availableSeats = seatStates.Count(ss => ss.State == SeatStatus.Free);
            var reservedSeats = seatStates.Count(ss => ss.State == SeatStatus.ReservedTemp);
            var soldSeats = seatStates.Count(ss => ss.State == SeatStatus.Paid);
            var fillRate = totalSeats > 0
                ? Math.Round((decimal)(totalSeats - availableSeats) / totalSeats * 100, 2)
                : 0;

            // Calcul des revenus
            var potentialRevenue = seatStates.Sum(ss => ss.Seat.PricingZone?.Price ?? 0);
            var actualRevenue = seatStates
                .Where(ss => ss.State == SeatStatus.Paid)
                .Sum(ss => ss.Seat.PricingZone?.Price ?? 0);

            // 4. MAPPING Entity -> DTO
            return new EventDto
            {
                Id = evt.Id,
                Name = evt.Name,
                Date = evt.Date,
                Type = evt.Type,
                Description = evt.Description,
                ImageEvent = evt.ImageEvent,
                VenueId = evt.VenueId,
                VenueName = evt.Venue?.Name ?? "N/A",
                VenueCapacity = evt.Venue?.Capacity ?? 0,
                TotalSeats = totalSeats,
                AvailableSeats = availableSeats,
                ReservedSeats = reservedSeats,
                SoldSeats = soldSeats,
                FillRate = fillRate,
                PotentialRevenue = potentialRevenue,
                ActualRevenue = actualRevenue
            };
        }

        // ============================================================
        // POST - CRÉER UN EVENT
        // ============================================================

        /// <summary>
        /// Crée un nouvel event
        /// LOGIQUE MÉTIER :
        /// - Mapper CreateEventDto -> Entity
        /// - Sauvegarder via Repository
        /// - Récupérer l'event créé avec stats
        /// - Retourner EventDto
        /// </summary>
        public async Task<EventDto> CreateEventAsync(CreateEventDto createDto)
        {
            // 1. MAPPING DTO -> Entity
            var newEvent = new Event
            {
                Name = createDto.Name,
                Date = createDto.Date,
                Type = createDto.Type,
                Description = createDto.Description,
                VenueId = createDto.VenueId,
                ImageEvent = createDto.ImageEvent
            };

            // 2. SAUVEGARDE (via Repository)
            await _eventRepository.AddAsync(newEvent);
            await _uow.SaveChangesAsync();

            // 2.5 CRÉATION DES ÉTATS DE SIÈGES pour cet event
            // Récupérer tous les sièges du venue
            var venueSeats = await _seatRepository.GetByVenueIdAsync(createDto.VenueId);

            // Créer un SeatReservationState (Free) pour chaque siège
            foreach (var seat in venueSeats)
            {
                var seatState = new SeatReservationState
                {
                    EventId = newEvent.Id,
                    SeatId = seat.Id,
                    State = SeatStatus.Free
                };
                await _seatStateRepository.AddAsync(seatState);
            }

            // Sauvegarder tous les états de sièges
            await _uow.SaveChangesAsync();

            // 3. RÉCUPÉRATION de l'event créé avec toutes les stats
            var result = await GetEventByIdAsync(newEvent.Id);
            return result!;
        }

        // ============================================================
        // PUT - METTRE À JOUR UN EVENT
        // ============================================================

        /// <summary>
        /// Met à jour un event existant
        /// LOGIQUE MÉTIER :
        /// - Vérifier que l'event existe
        /// - Appliquer les modifications (SAUF le venue qui est immuable)
        /// - Sauvegarder
        /// - Retourner l'event mis à jour avec stats
        /// </summary>
        public async Task<EventDto?> UpdateEventAsync(UpdateEventDto updateDto)
        {
            // 1. RÉCUPÉRATION de l'event existant
            var oldEvent = await _eventRepository.GetByIdAsync(updateDto.Id);
            if (oldEvent == null) return null;

            // 2. APPLICATION DES MODIFICATIONS
            // NOTE: Le venue n'est PAS modifiable (géré côté frontend - champ désactivé)
            oldEvent.Name = updateDto.Name;
            oldEvent.Date = updateDto.Date;
            oldEvent.Type = updateDto.Type;
            oldEvent.Description = updateDto.Description;
            // oldEvent.VenueId = updateDto.VenueId; // COMMENTÉ: Le venue est immuable
            oldEvent.ImageEvent = updateDto.ImageEvent;

            // 3. SAUVEGARDE
            _eventRepository.Update(oldEvent);
            await _uow.SaveChangesAsync();

            // 4. RÉCUPÉRATION avec stats mises à jour
            return await GetEventByIdAsync(oldEvent.Id);
        }

        // ============================================================
        // DELETE - SUPPRIMER UN EVENT
        // ============================================================

        /// <summary>
        /// Supprime un event
        /// LOGIQUE MÉTIER (VALIDATION) :
        /// - Vérifier que l'event existe
        /// - RÈGLE MÉTIER : On ne peut pas supprimer un event qui a des réservations !
        /// - Si OK, supprimer
        ///
        /// VOILÀ POURQUOI LE SERVICE EST IMPORTANT :
        /// Le Repository ne fait que .Remove(), il ne valide rien.
        /// Le Service applique les RÈGLES MÉTIER avant de supprimer.
        /// </summary>
        public async Task<bool> DeleteEventAsync(int id)
        {
            // 1. VÉRIFIER QUE L'EVENT EXISTE
            var eventToDelete = await _eventRepository.GetByIdAsync(id);
            if (eventToDelete == null) return false;

            // 2. RÈGLE MÉTIER : Vérifier s'il y a des réservations
            var hasReservations = await _eventRepository.HasReservationsAsync(id);
            if (hasReservations)
            {
                // On ne peut pas supprimer un event avec des réservations !
                // Le Controller devra retourner un 400 Bad Request avec un message d'erreur
                throw new InvalidOperationException(
                    "Impossible de supprimer cet événement car il possède des réservations.");
            }

            // 3. SI OK, SUPPRIMER
            _eventRepository.Remove(eventToDelete);
            await _uow.SaveChangesAsync();
            return true;
        }

        // ============================================================
        // MÉTHODES SUPPLÉMENTAIRES AVEC LOGIQUE MÉTIER
        // ============================================================

        public async Task<List<EventListDto>> GetUpcomingEventsAsync()
        {
            var events = await _eventRepository.GetUpcomingEventsAsync();
            return await MapToEventListDtos(events);
        }

        public async Task<List<EventListDto>> GetEventsByTypeAsync(string type)
        {
            var events = await _eventRepository.GetEventsByTypeAsync(type);
            return await MapToEventListDtos(events);
        }

        public async Task<List<EventListDto>> GetEventsByVenueAsync(int venueId)
        {
            var events = await _eventRepository.GetEventsByVenueIdAsync(venueId);
            return await MapToEventListDtos(events);
        }

        // ============================================================
        // MÉTHODE PRIVÉE DE MAPPING (réutilisable)
        // ============================================================

        /// <summary>
        /// Méthode helper privée pour mapper une liste d'Events vers EventListDto
        /// Évite la duplication de code
        /// </summary>
        private async Task<List<EventListDto>> MapToEventListDtos(List<Event> events)
        {
            var result = new List<EventListDto>();
            foreach (var evt in events)
            {
                var seatStates = await _seatStateRepository.GetByEventIdAsync(evt.Id);
                var totalSeats = seatStates.Count;
                var availableSeats = seatStates.Count(ss => ss.State == SeatStatus.Free);
                var fillRate = totalSeats > 0
                    ? Math.Round((decimal)(totalSeats - availableSeats) / totalSeats * 100, 2)
                    : 0;

                result.Add(new EventListDto
                {
                    Id = evt.Id,
                    Name = evt.Name,
                    Date = evt.Date,
                    Type = evt.Type,
                    ImageEvent = evt.ImageEvent,
                    VenueName = evt.Venue?.Name ?? "N/A",
                    AvailableSeats = availableSeats,
                    FillRate = fillRate
                });
            }
            return result;
        }

        
    }
}
