using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace incode2.DTO
{
    public class ImageUploadRequest
    {
        public int SessionId { get; set; }

        [Required]
        public IFormFile Image { get; set; } 

        [Required]
        public string ConnectionId { get; set; } // SignalR ID
    }
}