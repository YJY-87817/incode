using System.ComponentModel.DataAnnotations;

namespace incode2.DTO
{
    public class MessageRequest
    {
        public int SessionId { get; set; }
        public string UserContent { get; set; }
    }
}