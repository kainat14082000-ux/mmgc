using System.ComponentModel.DataAnnotations;

namespace MMGC.Models;

public class LabTestCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Category Name")]
    public string CategoryName { get; set; } = string.Empty; // Blood, Radiology, Pathology, Ultrasound, etc.

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ICollection<LabTest> LabTests { get; set; } = new List<LabTest>();
}
