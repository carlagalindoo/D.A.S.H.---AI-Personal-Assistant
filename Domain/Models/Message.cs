using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public enum IntentType
    {
        Create,
        Query,
        Update,
        Delete
    }
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime SentAt { get; set; }

        public IntentType Intent { get; set; }

        public string Sender { get; set; } = string.Empty;

        [ForeignKey("ChatHistoryId")]
        public int ChatHistoryId { get; set; }
    }
}
