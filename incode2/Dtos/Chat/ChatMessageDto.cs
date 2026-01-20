namespace incode2.Dtos.Chat
{
    public class ChatMessageDto //回傳用
    {
        public long ChatMessageId { get; set; }
        public int SessionId { get; set; }
        public string Role { get; set; }     
        public string Body { get; set; }      
        public string CreatedAt { get; set; } 
        public bool IsImage { get; set; }
    }
}
