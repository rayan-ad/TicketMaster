/**
 * Configuration d'environnement pour le développement
 * Centralise toutes les URLs et constantes de l'application
 */
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7287/api',
  signalRUrl: 'https://localhost:7287/hubs/seat',

  // Pagination
  defaultPageSize: 9,
  usersPageSize: 10,

  // Timeouts (millisecondes)
  toastDuration: 3000,
  toastDurationError: 6000,
  pdfRenderDelay: 100,

  // Reservation
  reservationExpirationMinutes: 15,

  // Zoom configuration
  minZoom: 0.6,
  maxZoom: 2.2,
  defaultZoom: 1,

  // PDF configuration
  pdfScale: 2,
  qrCodeSize: 200
};
