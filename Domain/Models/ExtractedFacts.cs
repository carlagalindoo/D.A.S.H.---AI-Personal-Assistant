using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class ExtractedFacts
    {
        public string? Action { get; set; }
        public string? Who { get; set; }
        public string? What { get; set; }

        public string? Where { get; set; }

        public string? When { get; set; }

        // UPDATE SUPPORT
        public string? TargetTask { get; set; }
        public string? NewTitle { get; set; }
        public string? NewWhen { get; set; }
        public string? NewWhere { get; set; }
        public string? NewWho { get; set; }
    }
}
