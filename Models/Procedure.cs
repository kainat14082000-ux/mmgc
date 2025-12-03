using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class Procedure
{
    [Key]
    public int Id { get; set; }

    // Make nullable to allow empty option from the select to bind to null.
    // Keep Required so validation still forces user to pick one.
    [Required(ErrorMessage = "Please select a patient.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid patient.")]
    [Display(Name = "Patient")]
    public int? PatientId { get; set; }

    [Required(ErrorMessage = "Please select a doctor.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid doctor.")]
    [Display(Name = "Doctor")]
    public int? DoctorId { get; set; }

    [Display(Name = "Nurse")]
    public int? NurseId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Procedure Name")]
    public string ProcedureName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Procedure Type")]
    public string ProcedureType { get; set; } = string.Empty;

    [Display(Name = "Procedure Date")]
    [DataType(DataType.DateTime)]
    public DateTime ProcedureDate { get; set; } = DateTime.Now;

    [StringLength(2000)]
    [Display(Name = "Treatment Notes")]
    public string? TreatmentNotes { get; set; }

    [StringLength(2000)]
    [Display(Name = "Prescription")]
    public string? Prescription { get; set; }

    [Display(Name = "Procedure Fee")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProcedureFee { get; set; }

    [StringLength(20)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Scheduled";

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }

    [StringLength(450)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    // Navigation properties (EF)
    [ForeignKey("PatientId")]
    public virtual Patient? Patient { get; set; }

    [ForeignKey("DoctorId")]
    public virtual Doctor? Doctor { get; set; }

    [ForeignKey("NurseId")]
    public virtual Nurse? Nurse { get; set; }

    public virtual ICollection<LabTest> LabTests { get; set; } = new List<LabTest>();
}
