using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class Transaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [StringLength(50)]
    [Display(Name = "Transaction Type")]
    public string TransactionType { get; set; } = string.Empty; // Appointment, LabTest, Procedure, Pharmacy, Other

    [Display(Name = "Appointment")]
    public int? AppointmentId { get; set; }

    [Display(Name = "Procedure")]
    public int? ProcedureId { get; set; }

    [Display(Name = "Lab Test")]
    public int? LabTestId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Amount")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Payment Mode")]
    public string PaymentMode { get; set; } = string.Empty; // Cash, Bank, Card, Online

    [StringLength(50)]
    [Display(Name = "Reference Number")]
    public string? ReferenceNumber { get; set; }

    [StringLength(20)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Completed"; // Pending, Completed, Refunded, Cancelled

    [Display(Name = "Transaction Date")]
    [DataType(DataType.DateTime)]
    public DateTime TransactionDate { get; set; } = DateTime.Now;

    [Display(Name = "Invoice Generated")]
    public bool InvoiceGenerated { get; set; }

    [StringLength(500)]
    [Display(Name = "Invoice Path")]
    public string? InvoicePath { get; set; }

    [Display(Name = "Payment Confirmation Sent")]
    public bool PaymentConfirmationSent { get; set; }

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [StringLength(450)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual Patient Patient { get; set; } = null!;

    [ForeignKey("AppointmentId")]
    public virtual Appointment? Appointment { get; set; }

    [ForeignKey("ProcedureId")]
    public virtual Procedure? Procedure { get; set; }

    [ForeignKey("LabTestId")]
    public virtual LabTest? LabTest { get; set; }
}
