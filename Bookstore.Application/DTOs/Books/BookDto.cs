
using Bookstore.Application.DTOs.Authors;

namespace Bookstore.Application.DTOs.Books;

public class BookDto
{
    public int BookId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? SubTitle { get; set; }

    public AuthorDto Author { get; set; } = new();
}
