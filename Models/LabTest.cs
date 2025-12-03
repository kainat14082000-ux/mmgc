using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class LabTest
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Required]
    [Display(Name = "Category")]
    public int LabTestCategoryId { get; set; }

    [Display(Name = "Procedure")]
    public int? ProcedureId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Test Name")]
    public string TestName { get; set; } = string.Empty;

    [Display(Name = "Test Date")]
    [DataType(DataType.DateTime)]
    public DateTime TestDate { get; set; } = DateTime.Now;

    [StringLength(20)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Pending"; // Pending, Sample Collected, In Progress, Completed, Cancelled

    [StringLength(450)]
    [Display(Name = "Assigned To")]
    public string? AssignedToUserId { get; set; } // Lab staff user ID

    [Display(Name = "Test Fee")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TestFee { get; set; }

    [StringLength(500)]
    [Display(Name = "Report File Path")]
    public string? ReportFilePath { get; set; } // PDF/Image/Text file path

    [StringLength(2000)]
    [Display(Name = "Report Notes")]
    public string? ReportNotes { get; set; }

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }

    [StringLength(450)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual Patient Patient { get; set; } = null!;

    [ForeignKey("LabTestCategoryId")]
    public virtual LabTestCategory LabTestCategory { get; set; } = null!;

    [ForeignKey("ProcedureId")]
    public virtual Procedure? Procedure { get; set; }
}
