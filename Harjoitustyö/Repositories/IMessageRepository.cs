using Harjoitustyö.Models;

namespace Harjoitustyö.Repositories
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetPublicMessagesAsync(int count);
        Task<IEnumerable<Message>> GetReceivedMessagesAsync(long userId);
        Task<IEnumerable<Message>> GetSentMessagesAsync(long userId);
        Task<IEnumerable<Message>> GetRepliesAsync(long messageId);
        Task<Message?> GetMessageAsync(long id);
        Task<Message?> AddMessageAsync(Message message);
        Task<bool> UpdateMessageAsync(Message message);
        Task<bool> DeleteMessageAsync(Message message);
    }
}
