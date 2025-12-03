using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class Patient
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "MR Number")]
    public string MRNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName => $"{FirstName} {LastName}";

    [Required]
    [StringLength(15)]
    [Display(Name = "Contact Number")]
    public string ContactNumber { get; set; } = string.Empty;

    [StringLength(15)]
    [Display(Name = "Alternate Contact")]
    public string? AlternateContact { get; set; }

    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required]
    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [NotMapped]
    public int Age => DateTime.Now.Year - DateOfBirth.Year - (DateTime.Now.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

    [Required]
    [StringLength(10)]
    [Display(Name = "Gender")]
    public string Gender { get; set; } = string.Empty; // Male, Female, Other

    [StringLength(500)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [StringLength(100)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [StringLength(50)]
    [Display(Name = "State")]
    public string? State { get; set; }

    [StringLength(10)]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [StringLength(500)]
    [Display(Name = "Medical History")]
    public string? MedicalHistory { get; set; }

    [StringLength(500)]
    [Display(Name = "Allergies")]
    public string? Allergies { get; set; }

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }

    // Navigation properties
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Procedure> Procedures { get; set; } = new List<Procedure>();
    public virtual ICollection<LabTest> LabTests { get; set; } = new List<LabTest>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
