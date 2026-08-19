using System.ComponentModel.DataAnnotations;

namespace ProductApi.DTOs;

public sealed class UpdateProductRequest
{
    [Required]
    [StringLength(120)]
    public string Name { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Price { get; init; }
}
