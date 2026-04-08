using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DAL.Repositories
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
