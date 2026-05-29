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
    public string SessionKey { get; set; } = "1";
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string Location { get; set; }
    public string People { get; set; }

}
