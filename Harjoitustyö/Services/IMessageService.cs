using Harjoitustyö.Models;

namespace Harjoitustyö.Services
{
    public interface IMessageService
    {
        Task<IEnumerable<MessageDTO>> GetPublicMessagesAsync(int count);
        Task<IEnumerable<MessageDTO>> GetReceivedMessagesAsync(string userName);
        Task<IEnumerable<MessageDTO>> GetSentMessagesAsync(string userName);
        Task<MessageDTO?> GetMessageAsync(long id, string viewer);
        Task<ServiceResult<IEnumerable<MessageDTO>>> GetRepliesAsync(long id, string viewer);
        Task<ServiceResult<MessageDTO>> NewMessageAsync(CreateMessageDTO message, string sender);
        Task<ServiceResult<MessageDTO>> UpdateMessageAsync(long id, UpdateMessageDTO message, string userName);
        Task<ServiceResult<bool>> DeleteMessageAsync(long id, string userName);
    }
}
