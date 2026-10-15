
using Bookstore.Application.DTOs.Books;
using Bookstore.Application.DTOs.Common;
using Bookstore.Application.Interfaces;
using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Data;
using Bookstore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Bookstore.Application.DTOs.Authors;

namespace Bookstore.Infrastructure.Services;

public class BookService : IBookService
{
    private readonly BookstoreDbContext _context;

    public BookService(BookstoreDbContext context)
    {
        _context = context;
    }

    public async Task<BookDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .AsNoTracking()
            .Where(b => b.BookId == id)
            .Select(b => new BookDto
            {
                BookId = b.BookId,
                Title = b.Title,
                SubTitle = b.SubTitle,
                Author = new AuthorDto
                {
                    AuthorId = b.Author.AuthorId,
                    Name = b.Author.Name
                }
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BookDto?> CreateAsync(
        CreateBookDto request,
        CancellationToken cancellationToken = default)
    {
        var authorExists = await _context.Authors
            .AnyAsync(a => a.AuthorId == request.AuthorId,
                cancellationToken);

        if (!authorExists)
            return null;

        var book = new Book
        {
            Title = request.Title,
            SubTitle = request.SubTitle,
            AuthorId = request.AuthorId
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(book.BookId, cancellationToken);
    }

    public async Task<BookDto?> UpdateAsync(
        int id,
        UpdateBookDto request,
        CancellationToken cancellationToken = default)
    {
        var book = await _context.Books
            .FirstOrDefaultAsync(b => b.BookId == id,
                cancellationToken);

        if (book is null)
            return null;

        var authorExists = await _context.Authors
            .AnyAsync(a => a.AuthorId == request.AuthorId,
                cancellationToken);

        if (!authorExists)
            throw new ArgumentException(
                "The specified author does not exist.",
                nameof(request));

        book.Title = request.Title;
        book.SubTitle = request.SubTitle;
        book.AuthorId = request.AuthorId;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(book.BookId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var book = await _context.Books
            .FirstOrDefaultAsync(b => b.BookId == id,
                cancellationToken);

        if (book is null)
            return false;

        _context.Books.Remove(book);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<PagedResultDto<BookDto>> SearchAsync(
        BookSearchDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Books
            .AsNoTracking()
            .AsQueryable();

        // Filter by book title
        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            var title = filter.Title.Trim().ToLower();

            query = query.Where(b =>
                b.Title.ToLower().Contains(title));
        }

        // Filter by author name
        if (!string.IsNullOrWhiteSpace(filter.Author))
        {
            var author = filter.Author.Trim().ToLower();

            query = query.Where(b =>
                b.Author.Name.ToLower().Contains(author));
        }

        // Count matching records
        var totalCount = await query.CountAsync(cancellationToken);

        // Calculate pagination
        var skip = (int)Math.Min(
            ((long)filter.Page - 1) * filter.PageSize,
            int.MaxValue);

        // Apply sorting and pagination
        var books = await query
            .OrderBy(b => b.BookId)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(b => new BookDto
            {
                BookId = b.BookId,
                Title = b.Title,
                SubTitle = b.SubTitle,
                Author = new AuthorDto
                {
                    AuthorId = b.Author.AuthorId,
                    Name = b.Author.Name
                }
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<BookDto>
        {
            Items = books,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }
}
