
using Bookstore.Application.DTOs.Books;
using Bookstore.Application.DTOs.Common;

namespace Bookstore.Application.Interfaces;

public interface IBookService
{
    Task<BookDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<BookDto?> CreateAsync(
        CreateBookDto request,
        CancellationToken cancellationToken = default);

    Task<BookDto?> UpdateAsync(
        int id,
        UpdateBookDto request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<BookDto>> SearchAsync(
        BookSearchDto filter,
        CancellationToken cancellationToken = default);

}
