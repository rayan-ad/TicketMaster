using System.ComponentModel.DataAnnotations;

namespace TicketMaster.DTOs
{
    /// <summary>
    /// DTO pour PUT (mettre à jour un Event)
    /// POURQUOI ?
    /// - Contient l'Id (pour savoir QUEL event modifier)
    /// - Validation des données
    /// - Évite de modifier accidentellement des propriétés sensibles
    /// </summary>
    public class UpdateEventDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "L'Id doit être valide")]
        public int Id { get; set; }

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
