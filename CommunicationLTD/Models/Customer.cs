using System.ComponentModel.DataAnnotations;

namespace CommunicationLTD.Models;

public class Customer
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    //public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    public int SectorId { get; set; }
    public Sector? Sector { get; set; }

    public int PackageId { get; set; }
    public InternetPackage? InternetPackage { get; set; }
}
