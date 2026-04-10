using System.Collections.Generic;

namespace Domain.Interfaces
{
    public interface ITaskRepository
    {
        System.Threading.Tasks.Task<List<Domain.Models.Task>> GetAllAsync();
        System.Threading.Tasks.Task<Domain.Models.Task?> GetByIdAsync(int id);
        System.Threading.Tasks.Task AddAsync(Domain.Models.Task task);
        System.Threading.Tasks.Task UpdateAsync(Domain.Models.Task task);
        System.Threading.Tasks.Task DeleteAsync(int id);
    }
}
