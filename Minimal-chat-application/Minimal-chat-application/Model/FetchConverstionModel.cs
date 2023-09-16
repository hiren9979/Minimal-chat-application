using System.ComponentModel.DataAnnotations;

namespace Minimal_chat_application.Model
{
    public class FetchConverstionModel
    {
        [Required]
        public string receiverId { get; set; }
        public string sort {  get; set; }

        public DateTime? time { get; set; }
        public int? count { get; set; }
    }
}
