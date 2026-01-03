using System.ComponentModel.DataAnnotations;

namespace TicketMaster.Models
{
    /// <summary>
    /// Enumeration of user roles in the system.
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Administrator with full system access.
        /// </summary>
        Admin,

        /// <summary>
        /// Event organizer who can manage venues and events.
        /// </summary>
        Organisateur,

        /// <summary>
        /// Regular client who can make reservations and purchase tickets.
        /// </summary>
        Client
    }

    /// <summary>
    /// Represents a user in the system.
    /// Stores user credentials, role information, and associated reservations.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier of the user.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Full name of the user.
        /// Maximum length: 100 characters.
        /// </summary>
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Email address of the user.
        /// Used for authentication and communication.
        /// Maximum length: 150 characters.
        /// </summary>
        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password of the user.
        /// Never stored in plain text for security.
        /// </summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Role assigned to the user.
        /// Defaults to Client.
        /// </summary>
        [Required]
        public UserRole Role { get; set; } = UserRole.Client;

        /// <summary>
        /// Collection of all reservations made by this user.
        /// </summary>
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
