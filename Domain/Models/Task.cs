using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models;

public class Task
{
    [Key]
    public int TaskId { get; set; }
    [Required, ForeignKey("UserId")]
    public int UserId { get; set; }
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
