using Harjoitustyö.Models;
using Harjoitustyö.Repositories;

namespace Harjoitustyö.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _repository;
        private readonly IUserRepository _userRepository;

        public MessageService(IMessageRepository repository, IUserRepository userRepository)
        {
            _repository = repository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<MessageDTO>> GetPublicMessagesAsync(int count)
        {
            IEnumerable<Message> messages = await _repository.GetPublicMessagesAsync(count);
            return messages.Select(MessageToDTO);
        }

        public async Task<IEnumerable<MessageDTO>> GetReceivedMessagesAsync(string userName)
        {
            User? user = await _userRepository.GetUserAsync(userName);
            if (user == null)
            {
                return Enumerable.Empty<MessageDTO>();
            }
            IEnumerable<Message> messages = await _repository.GetReceivedMessagesAsync(user.Id);
            return messages.Select(MessageToDTO);
        }

        public async Task<IEnumerable<MessageDTO>> GetSentMessagesAsync(string userName)
        {
            User? user = await _userRepository.GetUserAsync(userName);
            if (user == null)
            {
                return Enumerable.Empty<MessageDTO>();
            }
            IEnumerable<Message> messages = await _repository.GetSentMessagesAsync(user.Id);
            return messages.Select(MessageToDTO);
        }

        public async Task<MessageDTO?> GetMessageAsync(long id, string viewer)
        {
            Message? message = await _repository.GetMessageAsync(id);
            if (message == null || !CanView(message, viewer))
            {
                // Toisten yksityisviestien olemassaoloa ei paljasteta
                return null;
            }
            return MessageToDTO(message);
        }

        public async Task<ServiceResult<IEnumerable<MessageDTO>>> GetRepliesAsync(long id, string viewer)
        {
            Message? message = await _repository.GetMessageAsync(id);
            if (message == null || !CanView(message, viewer))
            {
                return ServiceResult<IEnumerable<MessageDTO>>.Fail(ServiceStatus.NotFound);
            }

            IEnumerable<Message> replies = await _repository.GetRepliesAsync(id);
            return ServiceResult<IEnumerable<MessageDTO>>.Ok(
                replies.Where(r => CanView(r, viewer)).Select(MessageToDTO).ToList());
        }

        public async Task<ServiceResult<MessageDTO>> NewMessageAsync(CreateMessageDTO dto, string senderName)
        {
            User? sender = await _userRepository.GetUserAsync(senderName);
            if (sender == null)
            {
                return ServiceResult<MessageDTO>.Fail(ServiceStatus.Forbidden);
            }

            Message message = new Message
            {
                Title = dto.Title,
                Body = dto.Body,
                SenderId = sender.Id,
                CreatedAt = DateTime.Now
            };

            if (!string.IsNullOrWhiteSpace(dto.Recipient))
            {
                User? recipient = await _userRepository.GetUserAsync(dto.Recipient);
                if (recipient == null)
                {
                    return ServiceResult<MessageDTO>.Fail(ServiceStatus.BadRequest, "Vastaanottajaa ei löydy");
                }
                message.RecipientId = recipient.Id;
            }

            if (dto.PreviousMessageId != null)
            {
                Message? previous = await _repository.GetMessageAsync(dto.PreviousMessageId.Value);
                if (previous == null || !CanView(previous, senderName))
                {
                    return ServiceResult<MessageDTO>.Fail(ServiceStatus.BadRequest, "Edellistä viestiä ei löydy");
                }
                message.PreviousMessageId = previous.Id;
            }

            Message? created = await _repository.AddMessageAsync(message);
            if (created == null)
            {
                return ServiceResult<MessageDTO>.Fail(ServiceStatus.Failed);
            }

            Message? stored = await _repository.GetMessageAsync(created.Id);
            return ServiceResult<MessageDTO>.Ok(MessageToDTO(stored ?? created));
        }

        public async Task<ServiceResult<MessageDTO>> UpdateMessageAsync(long id, UpdateMessageDTO dto, string userName)
        {
            Message? dbMessage = await _repository.GetMessageAsync(id);
            if (dbMessage == null || !CanView(dbMessage, userName))
            {
                return ServiceResult<MessageDTO>.Fail(ServiceStatus.NotFound);
            }
            if (dbMessage.Sender?.UserName != userName)
            {
                return ServiceResult<MessageDTO>.Fail(ServiceStatus.Forbidden, "Vain omia viestejä voi muokata");
            }

            dbMessage.Title = dto.Title;
            dbMessage.Body = dto.Body;
            dbMessage.EditedAt = DateTime.Now;

            return await _repository.UpdateMessageAsync(dbMessage)
                ? ServiceResult<MessageDTO>.Ok(MessageToDTO(dbMessage))
                : ServiceResult<MessageDTO>.Fail(ServiceStatus.Failed);
        }

        public async Task<ServiceResult<bool>> DeleteMessageAsync(long id, string userName)
        {
            Message? dbMessage = await _repository.GetMessageAsync(id);
            if (dbMessage == null || !CanView(dbMessage, userName))
            {
                return ServiceResult<bool>.Fail(ServiceStatus.NotFound);
            }
            if (dbMessage.Sender?.UserName != userName)
            {
                return ServiceResult<bool>.Fail(ServiceStatus.Forbidden, "Vain omia viestejä voi poistaa");
            }

            return await _repository.DeleteMessageAsync(dbMessage)
                ? ServiceResult<bool>.Ok(true)
                : ServiceResult<bool>.Fail(ServiceStatus.Failed);
        }

        private static bool CanView(Message message, string userName)
        {
            return message.RecipientId == null
                || message.Sender?.UserName == userName
                || message.Recipient?.UserName == userName;
        }

        private static MessageDTO MessageToDTO(Message message)
        {
            return new MessageDTO
            {
                Id = message.Id,
                Title = message.Title,
                Body = message.Body,
                Sender = message.Sender?.UserName,
                Recipient = message.Recipient?.UserName,
                PreviousMessageId = message.PreviousMessageId,
                IsPrivate = message.RecipientId != null,
                CreatedAt = message.CreatedAt,
                EditedAt = message.EditedAt
            };
        }
    }
}
