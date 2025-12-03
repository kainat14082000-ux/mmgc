using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class DoctorSchedule
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Doctor")]
    public int DoctorId { get; set; }

    [Required]
    [Display(Name = "Day of Week")]
    [StringLength(20)]
    public string DayOfWeek { get; set; } = string.Empty; // Monday, Tuesday, etc.

    [Required]
    [Display(Name = "Start Time")]
    [DataType(DataType.Time)]
    public TimeSpan StartTime { get; set; }

    [Required]
    [Display(Name = "End Time")]
    [DataType(DataType.Time)]
    public TimeSpan EndTime { get; set; }

    [Display(Name = "Is Available")]
    public bool IsAvailable { get; set; } = true;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("DoctorId")]
    public virtual Doctor Doctor { get; set; } = null!;
}
