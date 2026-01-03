namespace TicketMaster.Common
{
    /// <summary>
    /// Constantes globales de l'application TicketMaster.
    /// Centralise toutes les valeurs magiques pour faciliter la maintenance.
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// Configuration de pagination
        /// </summary>
        public static class Pagination
        {
            public const int DefaultEventPageSize = 9;
            public const int DefaultUserPageSize = 10;
            public const int DefaultVenuePageSize = 9;
            public const int MaxPageSize = 100;
        }

        /// <summary>
        /// Configuration des réservations
        /// </summary>
        public static class Reservations
        {
            public const int ExpirationMinutes = 15;
            public const int CleanupIntervalMinutes = 1;
        }

        /// <summary>
        /// Configuration JWT
        /// </summary>
        public static class Authentication
        {
            public const int TokenExpirationDays = 7;
            public const string JwtSecretKey = "VotreCleSecreteTresTresLongueEtSecurisee12345!";
            public const string JwtIssuer = "TicketMasterAPI";
            public const string JwtAudience = "TicketMasterClient";
        }

        /// <summary>
        /// Configuration PDF
        /// </summary>
        public static class Pdf
        {
            public const int QrCodeSize = 200;
            public const string QrCodeApiUrl = "https://api.qrserver.com/v1/create-qr-code/";
        }

        /// <summary>
        /// Messages d'erreur standardisés
        /// </summary>
        public static class ErrorMessages
        {
            public const string GenericError = "Une erreur est survenue.";
            public const string NotFound = "Ressource introuvable.";
            public const string Unauthorized = "Vous n'êtes pas autorisé à effectuer cette action.";
            public const string ValidationError = "Les données fournies sont invalides.";
            public const string ReservationNotFound = "Réservation introuvable.";
            public const string ReservationExpired = "Cette réservation a expiré.";
            public const string EventNotFound = "Événement introuvable.";
            public const string VenueNotFound = "Venue introuvable.";
            public const string UserNotFound = "Utilisateur introuvable.";
            public const string InvalidCredentials = "Email ou mot de passe incorrect.";
            public const string EmailAlreadyExists = "Cet email est déjà utilisé.";
        }

        /// <summary>
        /// Messages de succès standardisés
        /// </summary>
        public static class SuccessMessages
        {
            public const string Created = "Création réussie.";
            public const string Updated = "Mise à jour réussie.";
            public const string Deleted = "Suppression réussie.";
            public const string PaymentProcessed = "Paiement traité avec succès.";
            public const string ReservationCreated = "Réservation créée avec succès.";
            public const string ReservationCancelled = "Réservation annulée avec succès.";
        }

        /// <summary>
        /// Configuration CORS
        /// </summary>
        public static class Cors
        {
            public const string PolicyName = "AllowAngularApp";
            public static readonly string[] AllowedOrigins = { "http://localhost:4200" };
        }
    }
}
