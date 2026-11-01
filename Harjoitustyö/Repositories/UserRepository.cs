using Harjoitustyö.Data;
using Harjoitustyö.Models;
using Microsoft.EntityFrameworkCore;

namespace Harjoitustyö.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MessageServiceContext _context;

        public UserRepository(MessageServiceContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await _context.Users.OrderBy(u => u.UserName).ToListAsync();
        }

        public async Task<User?> GetUserAsync(string userName)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task<User?> GetUserAsync(long id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> AddUserAsync(User user)
        {
            _context.Users.Add(user);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return null;
            }
            return user;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return false;
            }
            return true;
        }

        public async Task<bool> DeleteUserAsync(User user)
        {
            // Käyttäjän mukana poistetaan kaikki viestit, joissa hän on lähettäjä tai vastaanottaja.
            // Muiden vastausviestien viittaus poistettaviin viesteihin nollataan ensin.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var messageIds = _context.Messages
                .Where(m => m.SenderId == user.Id || m.RecipientId == user.Id)
                .Select(m => m.Id);

            await _context.Messages
                .Where(m => m.PreviousMessageId != null && messageIds.Contains(m.PreviousMessageId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.PreviousMessageId, (long?)null));

            await _context.Messages
                .Where(m => m.SenderId == user.Id || m.RecipientId == user.Id)
                .ExecuteDeleteAsync();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }
    }
}
