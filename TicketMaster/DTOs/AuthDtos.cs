using System.ComponentModel.DataAnnotations;
using TicketMaster.Models;

namespace TicketMaster.DTOs
{
    /// <summary>
    /// DTO pour l'inscription d'un nouvel utilisateur
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(150, ErrorMessage = "L'email ne peut pas dépasser 150 caractères")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir entre 6 et 100 caractères")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Role par défaut: Client. Admin/Organisateur doivent être créés manuellement.
        /// </summary>
        public UserRole Role { get; set; } = UserRole.Client;
    }

    /// <summary>
    /// DTO pour la connexion d'un utilisateur
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO de réponse après authentification réussie
    /// </summary>
    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// DTO pour mettre à jour le rôle d'un utilisateur
    /// </summary>
    public class UpdateRoleDto
    {
        [Required(ErrorMessage = "Le rôle est requis")]
        public UserRole Role { get; set; }
    }
}
