using System.Security.Cryptography;
using Harjoitustyö.Models;
using Harjoitustyö.Repositories;

namespace Harjoitustyö.Services
{
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        private readonly IUserRepository _repository;

        public UserAuthenticationService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<User?> Authenticate(string userName, string password)
        {
            User? user = await _repository.GetUserAsync(userName);
            if (user == null)
            {
                return null;
            }

            byte[] hash = HashPassword(password, user.Salt);
            if (!CryptographicOperations.FixedTimeEquals(hash, user.Password))
            {
                return null;
            }

            user.LastLogin = DateTime.Now;
            await _repository.UpdateUserAsync(user);
            return user;
        }

        // Palautetaan sama olio, joka annettiin parametrina (ks. EF:n "olio on jo olemassa" -ongelma)
        public User CreateUserCredentials(User user, string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            user.Password = HashPassword(password, salt);
            user.Salt = salt;
            user.JoinDate = user.JoinDate != null ? user.JoinDate : DateTime.Now;
            user.LastLogin = DateTime.Now;

            return user;
        }

        private static byte[] HashPassword(string password, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        }
    }
}
