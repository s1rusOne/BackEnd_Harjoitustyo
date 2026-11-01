namespace Harjoitustyö.Models
{
    public class Message
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        public long SenderId { get; set; }
        public User? Sender { get; set; }

        // Jos vastaanottaja on null, viesti on julkinen
        public long? RecipientId { get; set; }
        public User? Recipient { get; set; }

        public long? PreviousMessageId { get; set; }
        public Message? PreviousMessage { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
    }
}
