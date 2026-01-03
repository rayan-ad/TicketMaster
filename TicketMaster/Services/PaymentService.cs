using System.Text.Json;
using TicketMaster.DTOs;
using TicketMaster.Models;
using TicketMaster.Repositories;
using TicketMaster.Enum;

namespace TicketMaster.Services
{
    /// <summary>
    /// DTO pour traiter un paiement
    /// </summary>
    public class ProcessPaymentDto
    {
        public int ReservationId { get; set; }
        public string PaymentMethod { get; set; } = "Card";
        public string? CardNumber { get; set; }
        public string? CardName { get; set; }
        public string? CardExpiry { get; set; }
        public string? CardCVV { get; set; }
    }

    public interface IPaymentService
    {
        Task<ReservationDto> ProcessPaymentAsync(ProcessPaymentDto dto, int userId);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IReservationRepository _reservationRepo;
        private readonly ISeatReservationStateRepository _seatStateRepo;
        private readonly ITicketRepository _ticketRepo;
        private readonly IUnitOfWork _unitOfWork;

        public PaymentService(
            IReservationRepository reservationRepo,
            ISeatReservationStateRepository seatStateRepo,
            ITicketRepository ticketRepo,
            IUnitOfWork unitOfWork)
        {
            _reservationRepo = reservationRepo;
            _seatStateRepo = seatStateRepo;
            _ticketRepo = ticketRepo;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Traite un paiement fictif pour une réservation
        /// LOGIQUE:
        /// 1. Valide que la réservation appartient à l'utilisateur
        /// 2. Valide que la réservation est en statut Pending
        /// 3. Crée un enregistrement Payment
        /// 4. Change le statut de la réservation en Paid
        /// 5. Change l'état des sièges de ReservedTemp à Paid
        /// 6. Génère les billets avec QR codes
        /// </summary>
        public async Task<ReservationDto> ProcessPaymentAsync(ProcessPaymentDto dto, int userId)
        {
            var reservation = await _reservationRepo.GetByIdAsync(dto.ReservationId);

            if (reservation == null)
            {
                throw new InvalidOperationException("Réservation introuvable.");
            }

            if (reservation.UserId != userId)
            {
                throw new UnauthorizedAccessException("Cette réservation ne vous appartient pas.");
            }

            if (reservation.Status != ReservationStatus.Pending)
            {
                throw new InvalidOperationException("Cette réservation ne peut pas être payée.");
            }

            if (reservation.ExpiresAt.HasValue && reservation.ExpiresAt.Value < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Cette réservation a expiré.");
            }

            var paymentMethod = dto.PaymentMethod.ToLower() switch
            {
                "card" => PaymentMethod.Card,
                "paypal" => PaymentMethod.Qr,
                "bancontact" => PaymentMethod.BankCard,
                _ => PaymentMethod.Card
            };

            var payment = new Payment
            {
                ReservationId = reservation.Id,
                Amount = reservation.TotalAmount,
                Method = paymentMethod,
                Reference = $"PAY-{DateTime.UtcNow.Ticks}-{reservation.Id}",
                Status = PaymentStatus.Succeeded,
                CreatedAt = DateTime.UtcNow,
                ConfirmedAt = DateTime.UtcNow
            };

            reservation.Payments.Add(payment);
            reservation.Status = ReservationStatus.Paid;
            await _reservationRepo.UpdateAsync(reservation);

            // OPTIMISATION: Préparer toutes les modifications en mémoire d'abord
            var ticketsToCreate = new List<Ticket>();
            var baseTimestamp = DateTime.UtcNow.Ticks;

            foreach (var rs in reservation.ReservationSeats)
            {
                // Mettre à jour l'état du siège directement (EF trackera le changement)
                var seatState = await _seatStateRepo.GetByEventAndSeatAsync(reservation.EventId, rs.SeatId);
                if (seatState != null)
                {
                    seatState.State = SeatStatus.Paid;
                    // Pas besoin de UpdateAsync, EF va tracker automatiquement
                }

                // Générer un ticketNumber unique avec index pour éviter les doublons
                var ticketNumber = $"TKT-{baseTimestamp}-{rs.SeatId}";

                var ticketData = new
                {
                    eventId = reservation.EventId,
                    eventName = reservation.Event.Name,
                    seatId = rs.SeatId,
                    row = rs.Seat.Row,
                    number = rs.Seat.Number,
                    zone = rs.Seat.PricingZone?.Name,
                    price = rs.PriceAtBooking,
                    ticketId = ticketNumber,
                    date = reservation.Event.Date
                };

                var qrData = JsonSerializer.Serialize(ticketData);
                var qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(qrData)}";

                ticketsToCreate.Add(new Ticket
                {
                    ReservationId = reservation.Id,
                    SeatId = rs.SeatId,
                    TicketNumber = ticketNumber,
                    QrCodeData = qrData,
                    QrCodeUrl = qrCodeUrl,
                    GeneratedAt = DateTime.UtcNow,
                    IsUsed = false
                });
            }

            // Créer tous les tickets en une seule opération
            foreach (var ticket in ticketsToCreate)
            {
                await _ticketRepo.CreateAsync(ticket);
            }

            // Un seul SaveChanges pour tout (payment + reservation + seats + tickets)
            await _unitOfWork.SaveChangesAsync();

            var updated = await _reservationRepo.GetByIdAsync(reservation.Id);
            return MapToDto(updated!);
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
                Tickets = reservation.Tickets.Select(t => new TicketDto
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
