namespace TicketMaster.Services
{
    /// <summary>
    /// Service pour hasher et vérifier les mots de passe.
    /// Utilise BCrypt pour un hash sécurisé avec salt automatique.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hash un mot de passe en clair.
        /// </summary>
        /// <param name="password">Mot de passe en clair</param>
        /// <returns>Hash BCrypt du mot de passe</returns>
        string HashPassword(string password);

        /// <summary>
        /// Vérifie si un mot de passe en clair correspond au hash.
        /// </summary>
        /// <param name="password">Mot de passe en clair</param>
        /// <param name="hash">Hash BCrypt stocké en base</param>
        /// <returns>True si le mot de passe est correct</returns>
        bool VerifyPassword(string password, string hash);
    }

    public class PasswordHasher : IPasswordHasher
    {
        /// <summary>
        /// Hash un mot de passe avec BCrypt (workFactor = 11)
        /// </summary>
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        }

        /// <summary>
        /// Vérifie un mot de passe contre un hash BCrypt
        /// </summary>
        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
