using System.ComponentModel.DataAnnotations;

namespace CommunicationLTD.Models;

public class Sector
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
