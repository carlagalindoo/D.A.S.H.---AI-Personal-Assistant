using DAL.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace D.A.S.H.Pages.Tasks
{
    public class EditModel : PageModel
    {
        private readonly ITaskRepository _taskRepository;

        [BindProperty]
        public Domain.Models.Task TaskItem { get; set; } = new();

        public EditModel(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public async System.Threading.Tasks.Task<IActionResult> OnGetAsync(int id)
        {
            var task = await _taskRepository.GetByIdAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            TaskItem = task;
            return Page();
        }

        public async System.Threading.Tasks.Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _taskRepository.UpdateAsync(TaskItem);
            return RedirectToPage("/Tasks/Index");
        }
    }
}
