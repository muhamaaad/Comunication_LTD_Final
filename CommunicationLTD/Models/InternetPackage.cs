using System.ComponentModel.DataAnnotations;

namespace CommunicationLTD.Models;

public class InternetPackage
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int SpeedMbps { get; set; }

    [Range(0, 10000)]
    public decimal Price { get; set; }

    [StringLength(250)]
    public string? Description { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
