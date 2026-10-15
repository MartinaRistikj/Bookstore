
using System.ComponentModel.DataAnnotations;

namespace Bookstore.Application.DTOs.Books;

public class BookSearchDto
{
    public string? Title { get; set; }

    public string? Author { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}
