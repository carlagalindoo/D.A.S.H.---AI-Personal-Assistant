using DAL.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace D.A.S.H.Pages.Tasks
{
    public class CreateModel : PageModel
    {
        private readonly ITaskRepository _taskRepository;

        [BindProperty]
        public Domain.Models.Task TaskItem { get; set; } = new();

        public CreateModel(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public void OnGet()
        {
        }

        public async System.Threading.Tasks.Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _taskRepository.AddAsync(TaskItem);
            return RedirectToPage("/Tasks/Index");
        }
    }
}
