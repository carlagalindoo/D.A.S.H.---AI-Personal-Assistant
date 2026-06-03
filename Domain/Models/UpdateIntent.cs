using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class UpdateIntent
    {
        public int TaskId { get; set; }
        public string? NewTitle { get; set; }
        public string? NewWhen { get; set; }
        public string? NewWhere { get; set; }
        public string? NewWho { get; set; }
    }
}
