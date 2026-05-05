using DAL.Repositories;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Domain.Interfaces;

namespace D.A.S.H.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly ITaskRepository _taskRepository;

        public List<Domain.Models.Task> Tasks { get; set; } = new();

        public IndexModel(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            Tasks = await _taskRepository.GetAllAsync();
        }
    }
}
