using System.ComponentModel.DataAnnotations;

namespace TicketMaster.DTOs
{
    /// <summary>
    /// DTO pour la mise à jour du profil utilisateur
    /// </summary>
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "L'email n'est pas valide")]
        [StringLength(200, ErrorMessage = "L'email ne peut pas dépasser 200 caractères")]
        public string Email { get; set; } = string.Empty;
    }
}
