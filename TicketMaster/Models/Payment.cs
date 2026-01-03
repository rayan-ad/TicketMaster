using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace TicketMaster.Models
{
    /// <summary>
    /// Enumeration of payment statuses.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// Payment has been initiated but not yet confirmed.
        /// </summary>
        Pending,

        /// <summary>
        /// Payment has been successfully processed.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Payment was declined or failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Payment was canceled by the user or timed out.
        /// </summary>
        Canceled
    }

    /// <summary>
    /// Enumeration of payment methods supported by the system.
    /// </summary>
    public enum PaymentMethod
    {
        /// <summary>
        /// Payment by credit card (test mode).
        /// </summary>
        Card,

        /// <summary>
        /// Payment by QR code.
        /// </summary>
        Qr,

        /// <summary>
        /// Payment by bank transfer (simulated).
        /// </summary>
        BankCard
    }

    /// <summary>
    /// Represents a payment transaction for a reservation.
    /// Tracks payment status, method, amount, and timestamps.
    /// </summary>
    public class Payment
    {
        /// <summary>
        /// Unique identifier of the payment.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Unique transaction reference number.
        /// Displayed in the UI and on ticket PDFs for user reference.
        /// Maximum length: 64 characters.
        /// </summary>
        [Required, MaxLength(64)]
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// Amount paid in this transaction.
        /// </summary>
        [Range(0, 999999)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Current status of the payment.
        /// Defaults to Pending.
        /// </summary>
        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        /// <summary>
        /// Payment method used.
        /// Defaults to Card.
        /// </summary>
        [Required]
        public PaymentMethod Method { get; set; } = PaymentMethod.Card;

        /// <summary>
        /// Timestamp when the payment was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the payment was confirmed.
        /// Null if payment has not been confirmed.
        /// </summary>
        public DateTime? ConfirmedAt { get; set; }

        /// <summary>
        /// Timestamp when the payment was canceled.
        /// Null if payment was not canceled.
        /// </summary>
        public DateTime? CanceledAt { get; set; }

        /// <summary>
        /// ID of the reservation associated with this payment.
        /// </summary>
        public int ReservationId { get; set; }

        /// <summary>
        /// Reference to the reservation.
        /// </summary>
        public Reservation Reservation { get; set; } = null!;
    }
}
