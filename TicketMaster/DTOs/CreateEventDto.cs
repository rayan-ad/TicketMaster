using System.ComponentModel.DataAnnotations;

namespace TicketMaster.DTOs
{
    /// <summary>
    /// DTO pour POST (créer un Event)
    /// POURQUOI ?
    /// - Validation des données AVANT d'arriver au Service
    /// - Ne contient PAS d'Id (généré par la DB)
    /// - Ne contient PAS de propriétés calculées
    /// - Protection contre le "mass assignment" (l'utilisateur ne peut pas forcer un Id)
    /// </summary>
    public class CreateEventDto
    {
        [Required(ErrorMessage = "Le nom de l'événement est requis")]
        [StringLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La date est requise")]
        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Le type est requis")]
        [StringLength(100, ErrorMessage = "Le type ne peut pas dépasser 100 caractères")]
        public string Type { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "La description ne peut pas dépasser 1000 caractères")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Le lieu (Venue) est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "Le VenueId doit être valide")]
        public int VenueId { get; set; }

        [StringLength(500, ErrorMessage = "L'URL de l'image ne peut pas dépasser 500 caractères")]
        [Url(ErrorMessage = "L'URL de l'image n'est pas valide")]
        public string? ImageEvent { get; set; }
    }
}
