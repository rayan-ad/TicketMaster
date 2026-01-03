using TicketMaster.Models;
using TicketMaster.Repositories;

namespace TicketMaster.Services
{
    /// <summary>
    /// Service for managing user data.
    /// Provides functionality to retrieve user information.
    /// </summary>
    public class UserService : IUserService
    {
        /// <summary>
        /// Retrieves all users from the repository.
        /// </summary>
        /// <returns>A list of all users in the system.</returns>
        public List<User> GetUsers()
        {
           return UserRepository.Users;
        }
    }
}
