
using System.ComponentModel.DataAnnotations;

namespace Bookstore.Application.DTOs.Books;

public class UpdateBookDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    public string? SubTitle { get; set; }

    [Range(1, int.MaxValue)]
    public int AuthorId { get; set; }
}
