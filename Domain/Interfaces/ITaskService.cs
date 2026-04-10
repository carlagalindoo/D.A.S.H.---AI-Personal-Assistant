using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = Microsoft.Exchange.WebServices.Data.Task;

namespace Domain.Interfaces
{
    public interface ITaskService
    {
        Task<Task> CreateTaskAsync(Task task);
        Task<List<Task>> GetTaskListAsync(int userId);
        Task<Task> GetTaskByIdAsync(int id);
        Task<Task> UpdateTaskAsync(Task task);
        Task DeleteTaskAsync(int id);
    }
}
