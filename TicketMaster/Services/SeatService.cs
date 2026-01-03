using TicketMaster.DTOs;
using TicketMaster.Enum;
using TicketMaster.Models;
using TicketMaster.Repositories;

namespace TicketMaster.Services
{
    /// <summary>
    /// Service Seat - LOGIQUE MÉTIER
    /// RESPONSABILITÉS :
    /// - Récupérer les sièges via Repository
    /// - MAPPER Seat + SeatReservationState -> SeatDto avec infos de zone tarifaire
    /// - Gérer la réservation temporaire (hold/release) PAR EVENT
    /// - Valider les règles métier
    /// </summary>
    public class SeatService : ISeatService
    {
        private readonly ISeatRepository _repo;
        private readonly IEventRepository _eventRepository;
        private readonly ISeatReservationStateRepository _stateRepo;
        private readonly IUnitOfWork _unitOfWork;

        public SeatService(
            ISeatRepository repo,
            IEventRepository eventRepository,
            ISeatReservationStateRepository stateRepo,
            IUnitOfWork uwn)
        {
            _repo = repo;
            _eventRepository = eventRepository;
            _stateRepo = stateRepo;
            _unitOfWork = uwn;
        }

        /// <summary>
        /// Récupère tous les sièges d'un event avec mapping vers DTO
        /// NOUVELLE LOGIQUE (avec SeatReservationState) :
        /// 1. Récupérer les états de réservation pour cet event
        /// 2. MAPPER SeatReservationState -> SeatDto (inclut Seat + PricingZone + State)
        /// </summary>
        public async Task<List<SeatDto>> GetSeatsForEventAsync(int eventId)
        {
            // 1. Récupérer les états de sièges pour cet event
            // (Inclut déjà Seat et PricingZone via Include dans le repository)
            var seatStates = await _stateRepo.GetByEventIdAsync(eventId);

            // 2. MAPPER vers SeatDto
            return seatStates.Select(ss => MapToDto(ss)).ToList();
        }

        /// <summary>
        /// Récupère les sièges d'une zone tarifaire pour un event spécifique
        /// </summary>
        public async Task<List<SeatDto>> GetSeatsByPricingZoneAsync(int eventId, int pricingZoneId)
        {
            // Récupérer tous les états de sièges pour l'event
            var seatStates = await _stateRepo.GetByEventIdAsync(eventId);

            // Filtrer par zone tarifaire
            var filtered = seatStates.Where(ss => ss.Seat.PricingZoneId == pricingZoneId).ToList();

            return filtered.Select(ss => MapToDto(ss)).ToList();
        }

        /// <summary>
        /// Réserve temporairement un siège pour un event spécifique
        /// RÈGLE MÉTIER : On ne peut réserver que si état = Free
        /// </summary>
        public async Task<bool> HoldSeatAsync(int eventId, int seatId, int ttlMinutes = 15)
        {
            // Récupérer l'état du siège pour cet event
            var seatState = await _stateRepo.GetByEventAndSeatAsync(eventId, seatId);
            if (seatState == null || seatState.State != SeatStatus.Free)
            {
                return false; // État introuvable ou déjà réservé
            }

            // Changer l'état
            seatState.State = SeatStatus.ReservedTemp;
            seatState.ReservedAt = DateTime.UtcNow;
            seatState.ExpiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes);
            await _stateRepo.UpdateAsync(seatState);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Libère un siège réservé temporairement pour un event
        /// RÈGLE MÉTIER : On ne peut libérer que si état = ReservedTemp
        /// </summary>
        public async Task<bool> ReleaseSeatAsync(int eventId, int seatId)
        {
            // Récupérer l'état du siège pour cet event
            var seatState = await _stateRepo.GetByEventAndSeatAsync(eventId, seatId);
            if (seatState == null || seatState.State != SeatStatus.ReservedTemp)
            {
                return false; // État introuvable ou pas en réservation temporaire
            }

            // Libérer
            seatState.State = SeatStatus.Free;
            seatState.ReservedAt = null;
            seatState.ExpiresAt = null;
            seatState.UserId = null;
            await _stateRepo.UpdateAsync(seatState);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        // ============================================================
        // MÉTHODE PRIVÉE DE MAPPING
        // ============================================================

        /// <summary>
        /// Mappe un SeatReservationState vers SeatDto
        /// IMPORTANT : Combine les infos du Seat, PricingZone et State par event
        /// </summary>
        private SeatDto MapToDto(SeatReservationState seatState)
        {
            var seat = seatState.Seat;
            var zone = seat.PricingZone;

            return new SeatDto
            {
                Id = seat.Id,
                Row = seat.Row ?? "?",
                Number = seat.Number,
                State = seatState.State.ToString(),
                PricingZoneId = seat.PricingZoneId,
                ZoneName = zone?.Name ?? "Unknown",
                ZoneColor = zone?.Color ?? "#999999",
                Price = zone?.Price ?? 0m,
                Zone = zone != null ? new ZoneDto
                {
                    Id = zone.Id,
                    Name = zone.Name,
                    Color = zone.Color,
                    Price = zone.Price
                } : null
            };
        }
    }
}
