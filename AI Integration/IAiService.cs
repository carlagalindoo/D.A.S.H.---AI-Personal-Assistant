using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AI_Integration
{
    public interface IAiService
    {
        Task<Domain.Models.ExtractedFacts?> ExtractFactsAsync(string userInput);
        Task<Domain.Models.UpdateIntent?> ExtractUpdateIntentAsync(string userInput, IEnumerable<Domain.Models.Task> existingTasks);
    }
}
