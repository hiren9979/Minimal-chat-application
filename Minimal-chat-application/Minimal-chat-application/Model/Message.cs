namespace Minimal_chat_application.Model
{
    public class Message
    {
        public int Id { get; set; }
        public String SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
