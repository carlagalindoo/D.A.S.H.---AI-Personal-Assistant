using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models;

public class Task
{
    [Key]
    public int Taskid { get; set; }
    [Required]
    public string Title { get; set; }
    [Required]
    public string Description { get; set; }
    [Required]
    public DateTime DueDate { get; set; }
    [Required]
    public string Location { get; set; }
    [Required]
    public string People { get; set; }

}
