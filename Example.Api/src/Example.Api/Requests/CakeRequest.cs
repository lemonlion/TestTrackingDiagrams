using System.ComponentModel.DataAnnotations;

namespace Example.Api.Requests;

public class CakeRequest
{
    [Required]
    public string? Milk { get; set; }

    [Required]
    public string? Flour { get; set; }

    [Required]
    public string? Eggs { get; set; }
}