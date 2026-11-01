using Harjoitustyö.Data;
using Harjoitustyö.Models;
using Microsoft.EntityFrameworkCore;

namespace Harjoitustyö.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly MessageServiceContext _context;

        public MessageRepository(MessageServiceContext context)
        {
            _context = context;
        }

        private IQueryable<Message> MessagesWithUsers =>
            _context.Messages.Include(m => m.Sender).Include(m => m.Recipient);

        public async Task<IEnumerable<Message>> GetPublicMessagesAsync(int count)
        {
            return await MessagesWithUsers
                .Where(m => m.RecipientId == null)
                .OrderByDescending(m => m.Id)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetReceivedMessagesAsync(long userId)
        {
            return await MessagesWithUsers
                .Where(m => m.RecipientId == userId)
                .OrderByDescending(m => m.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetSentMessagesAsync(long userId)
        {
            return await MessagesWithUsers
                .Where(m => m.SenderId == userId)
                .OrderByDescending(m => m.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetRepliesAsync(long messageId)
        {
            return await MessagesWithUsers
                .Where(m => m.PreviousMessageId == messageId)
                .OrderBy(m => m.Id)
                .ToListAsync();
        }

        public async Task<Message?> GetMessageAsync(long id)
        {
            return await MessagesWithUsers.FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Message?> AddMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return null;
            }
            return message;
        }

        public async Task<bool> UpdateMessageAsync(Message message)
        {
            _context.Messages.Update(message);
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

        public async Task<bool> DeleteMessageAsync(Message message)
        {
            // Vastaukset jäävät talteen, mutta niiden linkki poistettuun viestiin katkaistaan
            await _context.Messages
                .Where(m => m.PreviousMessageId == message.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.PreviousMessageId, (long?)null));

            _context.Messages.Remove(message);
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
    }
}
