using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class Prescription
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Required]
    [Display(Name = "Doctor")]
    public int DoctorId { get; set; }

    [Display(Name = "Appointment")]
    public int? AppointmentId { get; set; }

    [Display(Name = "Procedure")]
    public int? ProcedureId { get; set; }

    [Required]
    [StringLength(2000)]
    [Display(Name = "Prescription Details")]
    public string PrescriptionDetails { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Instructions")]
    public string? Instructions { get; set; }

    [Display(Name = "Prescription Date")]
    [DataType(DataType.DateTime)]
    public DateTime PrescriptionDate { get; set; } = DateTime.Now;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [StringLength(450)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual Patient Patient { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public virtual Doctor Doctor { get; set; } = null!;

    [ForeignKey("AppointmentId")]
    public virtual Appointment? Appointment { get; set; }

    [ForeignKey("ProcedureId")]
    public virtual Procedure? Procedure { get; set; }
}
