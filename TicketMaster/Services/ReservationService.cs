using TicketMaster.DTOs;
using TicketMaster.Models;
using TicketMaster.Repositories;
using TicketMaster.Enum;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TicketMaster.Hubs;

namespace TicketMaster.Services
{
    public interface IReservationService
    {
        Task<ReservationDto?> GetByIdAsync(int id, int userId);
        Task<List<ReservationDto>> GetMyReservationsAsync(int userId);
        Task<ReservationDto> CreateReservationAsync(CreateReservationDto dto, int userId);
        Task<bool> CancelReservationAsync(int reservationId, int userId);
        Task ReleaseExpiredReservationsAsync();
    }

    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepo;
        private readonly ISeatReservationStateRepository _seatStateRepo;
        private readonly IEventRepository _eventRepo;
        private readonly ISeatRepository _seatRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly DataAccess.TicketMasterContext _context;
        private readonly IHubContext<SeatHub> _hubContext;

        public ReservationService(
            IReservationRepository reservationRepo,
            ISeatReservationStateRepository seatStateRepo,
            IEventRepository eventRepo,
            ISeatRepository seatRepo,
            IUnitOfWork unitOfWork,
            DataAccess.TicketMasterContext context,
            IHubContext<SeatHub> hubContext)
        {
            _reservationRepo = reservationRepo;
            _seatStateRepo = seatStateRepo;
            _eventRepo = eventRepo;
            _seatRepo = seatRepo;
            _unitOfWork = unitOfWork;
            _context = context;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Récupère une réservation par ID si elle appartient à l'utilisateur
        /// </summary>
        public async Task<ReservationDto?> GetByIdAsync(int id, int userId)
        {
            var reservation = await _reservationRepo.GetByIdAsync(id);
            if (reservation == null || reservation.UserId != userId)
            {
                return null;
            }

            return MapToDto(reservation);
        }

        /// <summary>
        /// Récupère toutes les réservations d'un utilisateur
        /// </summary>
        public async Task<List<ReservationDto>> GetMyReservationsAsync(int userId)
        {
            var reservations = await _reservationRepo.GetByUserIdAsync(userId);
            return reservations.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Crée une nouvelle réservation avec sièges temporairement bloqués
        /// RÈGLES:
        /// - Tous les sièges doivent être libres
        /// - Bloque les sièges pour 15 minutes
        /// - Calcule le montant total
        /// </summary>
        public async Task<ReservationDto> CreateReservationAsync(CreateReservationDto dto, int userId)
        {
            var eventEntity = await _eventRepo.GetByIdAsync(dto.EventId);
            if (eventEntity == null)
            {
                throw new InvalidOperationException("Événement introuvable.");
            }

            // ✅ ANNULER toutes les réservations Pending existantes de cet utilisateur pour cet événement
            // (pour éviter d'avoir plusieurs réservations en parallèle)
            var existingReservations = await _reservationRepo.GetByEventIdAsync(dto.EventId);
            var userPendingReservations = existingReservations
                .Where(r => r.UserId == userId && r.Status == ReservationStatus.Pending)
                .ToList();

            foreach (var oldReservation in userPendingReservations)
            {
                Console.WriteLine($"[CLEANUP] Auto suppression de l'ancienne réservation {oldReservation.Id} avant d'en créer une nouvelle");

                // Libérer les sièges de l'ancienne réservation
                if (oldReservation.ReservationSeats != null)
                {
                    foreach (var rs in oldReservation.ReservationSeats)
                    {
                        var seatState = await _seatStateRepo.GetByEventAndSeatAsync(oldReservation.EventId, rs.SeatId);
                        if (seatState != null && seatState.State == SeatStatus.ReservedTemp)
                        {
                            seatState.State = SeatStatus.Free;
                            seatState.UserId = null;
                            seatState.ReservedAt = null;
                            seatState.ExpiresAt = null;
                            await _seatStateRepo.UpdateAsync(seatState);
                        }
                    }
                }

                await _reservationRepo.DeleteAsync(oldReservation.Id);
            }

            var seatStates = new List<SeatReservationState>();
            decimal totalAmount = 0;

            foreach (var seatId in dto.SeatIds)
            {
                var seatState = await _seatStateRepo.GetByEventAndSeatAsync(dto.EventId, seatId);
                if (seatState == null)
                {
                    throw new InvalidOperationException($"Le siège {seatId} est introuvable.");
                }

                // ✅ AUTORISER si le siège est Free OU si ReservedTemp par le MÊME utilisateur
                // (pour gérer le cas où l'utilisateur ajoute des sièges à sa réservation)
                bool isAvailable = seatState.State == SeatStatus.Free ||
                                   (seatState.State == SeatStatus.ReservedTemp && seatState.UserId == userId);

                if (!isAvailable)
                {
                    throw new InvalidOperationException($"Le siège {seatId} n'est pas disponible.");
                }

                seatStates.Add(seatState);
                totalAmount += seatState.Seat.PricingZone?.Price ?? 0;
            }

            var reservation = new Reservation
            {
                UserId = userId,
                EventId = dto.EventId,
                Status = ReservationStatus.Pending,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            await _reservationRepo.CreateAsync(reservation);

            // OPTIMISATION: Regrouper toutes les modifications avant le SaveChanges
            foreach (var seatState in seatStates)
            {
                var seat = seatState.Seat;
                reservation.ReservationSeats.Add(new ReservationSeat
                {
                    ReservationId = reservation.Id,
                    SeatId = seat.Id,
                    PriceAtBooking = seat.PricingZone?.Price ?? 0
                });

                seatState.State = SeatStatus.ReservedTemp;
                seatState.UserId = userId;
                seatState.ReservedAt = DateTime.UtcNow;
                seatState.ExpiresAt = reservation.ExpiresAt;
                // PAS de SaveChanges ici - juste marquer comme modifié
                await _seatStateRepo.UpdateAsync(seatState);
            }

            // UN SEUL SaveChanges pour tout
            await _unitOfWork.SaveChangesAsync();

            // NOTIFIER tous les clients via SignalR que les sièges ont changé
            Console.WriteLine($"[SIGNALR] Envoi SeatStatusChanged - Event:{dto.EventId}, Seats:[{string.Join(",", dto.SeatIds)}], User:{userId}, Status:ReservedTemp");
            await _hubContext.Clients.All.SendAsync("SeatStatusChanged", new
            {
                eventId = dto.EventId,
                seatIds = dto.SeatIds,
                status = "ReservedTemp",
                userId = userId
            });

            // OPTIMISATION: Utiliser AsNoTracking pour éviter le tracking EF
            var created = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Event)
                .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                        .ThenInclude(s => s.PricingZone)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == reservation.Id);

            return MapToDto(created!);
        }

        /// <summary>
        /// Annule une réservation (seulement si Pending)
        /// Libère les sièges associés
        /// </summary>
        public async Task<bool> CancelReservationAsync(int reservationId, int userId)
        {
            try
            {
                Console.WriteLine($"[LOOKUP] Annulation réservation {reservationId} pour user {userId}");

                var reservation = await _reservationRepo.GetByIdAsync(reservationId);
                if (reservation == null)
                {
                    Console.WriteLine($"[WARNING] Réservation {reservationId} introuvable - déjà supprimée");
                    return true;
                }

                Console.WriteLine($"[SUCCESS] Réservation trouvée: Event={reservation.EventId}, Status={reservation.Status}, Seats={reservation.ReservationSeats?.Count ?? 0}");

                if (reservation.UserId != userId)
                {
                    Console.WriteLine($"[ERROR] User {userId} ne peut pas annuler réservation de user {reservation.UserId}");
                    return false;
                }

                if (reservation.Status != ReservationStatus.Pending)
                {
                    Console.WriteLine($"[ERROR] Réservation status={reservation.Status}, annulation impossible");
                    return false;
                }

                // Libérer les sièges avant de supprimer la réservation
                var seatIdsToRelease = reservation.ReservationSeats?.Select(rs => rs.SeatId).ToList() ?? new List<int>();
                var eventId = reservation.EventId;

                Console.WriteLine($"[RELEASE] Libération de {seatIdsToRelease.Count} siège(s)");

                if (reservation.ReservationSeats != null)
                {
                    foreach (var rs in reservation.ReservationSeats)
                    {
                        var seatState = await _seatStateRepo.GetByEventAndSeatAsync(reservation.EventId, rs.SeatId);
                        if (seatState != null && seatState.State == SeatStatus.ReservedTemp)
                        {
                            seatState.State = SeatStatus.Free;
                            seatState.UserId = null;
                            seatState.ReservedAt = null;
                            seatState.ExpiresAt = null;
                            await _seatStateRepo.UpdateAsync(seatState);
                        }
                    }
                }

                // SUPPRIMER complètement la réservation
                bool deletionSucceeded = false;
                try
                {
                    await _reservationRepo.DeleteAsync(reservation.Id);
                    await _unitOfWork.SaveChangesAsync();
                    deletionSucceeded = true;
                    Console.WriteLine($"[SUCCESS] Réservation {reservationId} supprimée avec succès");
                }
                catch (DbUpdateConcurrencyException)
                {
                    // La réservation a déjà été supprimée - c'est OK
                    Console.WriteLine($"[WARNING] Réservation {reservationId} déjà supprimée par un autre processus");
                    deletionSucceeded = true; // Considérer comme succès
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] ERREUR lors de la suppression de la réservation {reservationId}: {ex.Message}");
                    Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                    // NE PAS continuer - rethrow l'exception
                    throw;
                }

                // NOTIFIER tous les clients que les sièges sont libérés (seulement si suppression réussie)
                if (deletionSucceeded)
                {
                    Console.WriteLine($"[SIGNALR] Envoi SeatStatusChanged - Event:{eventId}, Seats:[{string.Join(",", seatIdsToRelease)}], Status:Free");
                    await _hubContext.Clients.All.SendAsync("SeatStatusChanged", new
                    {
                        eventId = eventId,
                        seatIds = seatIdsToRelease,
                        status = "Free"
                    });
                }

                return deletionSucceeded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur globale CancelReservationAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Libère automatiquement les réservations expirées
        /// Appelé par le background service
        /// </summary>
        public async Task ReleaseExpiredReservationsAsync()
        {
            var expired = await _reservationRepo.GetExpiredReservationsAsync();

            if (expired.Any())
            {
                Console.WriteLine($"[TIMER] Libération de {expired.Count} réservation(s) expirée(s)");
            }

            foreach (var reservation in expired)
            {
                var seatIdsToRelease = reservation.ReservationSeats.Select(rs => rs.SeatId).ToList();

                foreach (var rs in reservation.ReservationSeats)
                {
                    var seatState = await _seatStateRepo.GetByEventAndSeatAsync(reservation.EventId, rs.SeatId);
                    if (seatState != null)
                    {
                        seatState.State = SeatStatus.Free;
                        seatState.UserId = null;
                        seatState.ReservedAt = null;
                        seatState.ExpiresAt = null;
                        await _seatStateRepo.UpdateAsync(seatState);
                    }
                }

                // SUPPRIMER complètement les réservations expirées
                await _reservationRepo.DeleteAsync(reservation.Id);

                // NOTIFIER tous les clients que les sièges sont libérés
                Console.WriteLine($"[SIGNALR] Envoi SeatStatusChanged (expiration) - Event:{reservation.EventId}, Seats:[{string.Join(",", seatIdsToRelease)}], Status:Free");
                await _hubContext.Clients.All.SendAsync("SeatStatusChanged", new
                {
                    eventId = reservation.EventId,
                    seatIds = seatIdsToRelease,
                    status = "Free"
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Mappe Reservation vers ReservationDto
        /// </summary>
        private ReservationDto MapToDto(Reservation reservation)
        {
            return new ReservationDto
            {
                Id = reservation.Id,
                UserId = reservation.UserId,
                UserName = reservation.User?.Name ?? "",
                EventId = reservation.EventId,
                EventName = reservation.Event?.Name ?? "",
                EventDate = reservation.Event?.Date ?? DateTime.MinValue,
                Status = reservation.Status.ToString(),
                TotalAmount = reservation.TotalAmount,
                CreatedAt = reservation.CreatedAt,
                ExpiresAt = reservation.ExpiresAt,
                Seats = reservation.ReservationSeats.Select(rs => new ReservationSeatDto
                {
                    SeatId = rs.SeatId,
                    Row = rs.Seat.Row,
                    Number = rs.Seat.Number,
                    ZoneName = rs.Seat.PricingZone?.Name ?? "",
                    PriceAtBooking = rs.PriceAtBooking
                }).ToList(),
                Tickets = reservation.Tickets?.Select(t => new TicketDto
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    QrCodeUrl = t.QrCodeUrl,
                    QrCodeData = t.QrCodeData,
                    GeneratedAt = t.GeneratedAt,
                    IsUsed = t.IsUsed,
                    SeatId = t.SeatId,
                    Row = t.Seat.Row,
                    Number = t.Seat.Number,
                    ZoneName = t.Seat.PricingZone?.Name ?? "",
                    Price = reservation.ReservationSeats.FirstOrDefault(rs => rs.SeatId == t.SeatId)?.PriceAtBooking ?? 0,
                    EventName = reservation.Event?.Name ?? "",
                    EventDate = reservation.Event?.Date ?? DateTime.MinValue
                }).ToList()
            };
        }
    }
}
