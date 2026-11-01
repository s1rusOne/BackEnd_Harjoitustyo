using System.ComponentModel.DataAnnotations;

namespace Harjoitustyö.Models
{
    public class MessageDTO
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Sender { get; set; }
        public string? Recipient { get; set; }
        public long? PreviousMessageId { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
    }

    public class CreateMessageDTO
    {
        [Required, StringLength(100, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000, MinimumLength = 1)]
        public string Body { get; set; } = string.Empty;

        public string? Recipient { get; set; }

        public long? PreviousMessageId { get; set; }
    }

    public class UpdateMessageDTO
    {
        [Required, StringLength(100, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000, MinimumLength = 1)]
        public string Body { get; set; } = string.Empty;
    }
}
