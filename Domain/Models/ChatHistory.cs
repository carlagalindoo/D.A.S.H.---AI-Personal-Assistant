using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class ChatHistory
    {
        [Key]
        public int ChatHistoryId { get; set; }
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public DateTime SessionStart { get; set; }

        public List<Message> Messages { get; set; } = new List<Message>();
    }

}
