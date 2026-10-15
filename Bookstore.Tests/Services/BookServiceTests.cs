
using Bookstore.Application.DTOs.Books;
using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Data;
using Bookstore.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Tests.Services;

public class BookServiceTests
{
    private static async Task<(
        SqliteConnection Connection,
        BookstoreDbContext Context,
        BookService Service)> CreateTestEnvironmentAsync()
    {
        var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BookstoreDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new BookstoreDbContext(options);

        await context.Database.EnsureCreatedAsync();

        context.Authors.AddRange(
            new Author
            {
                AuthorId = 1,
                Name = "Robert C. Martin"
            },
            new Author
            {
                AuthorId = 2,
                Name = "Martin Fowler"
            });

        context.Books.AddRange(
            new Book
            {
                BookId = 1,
                Title = "Clean Code",
                AuthorId = 1
            },
            new Book
            {
                BookId = 2,
                Title = "Clean Architecture",
                AuthorId = 1
            },
            new Book
            {
                BookId = 3,
                Title = "Refactoring",
                AuthorId = 2
            });

        await context.SaveChangesAsync();

        return (connection, context, new BookService(context));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingBook_ReturnsBook()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Clean Code", result.Title);
        Assert.Equal("Robert C. Martin", result.Author.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonexistentBook_ReturnsNull()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ValidBook_CreatesBook()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var request = new CreateBookDto
        {
            Title = "New Test Book",
            SubTitle = "Test Subtitle",
            AuthorId = 1
        };

        var result = await service.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("New Test Book", result.Title);

        Assert.True(await context.Books.AnyAsync(
            b => b.BookId == result.BookId));
    }

    [Fact]
    public async Task CreateAsync_InvalidAuthor_ReturnsNull()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var request = new CreateBookDto
        {
            Title = "Test Book",
            AuthorId = 999
        };

        var result = await service.CreateAsync(request);

        Assert.Null(result);
        Assert.Equal(3, await context.Books.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_ExistingBook_UpdatesBook()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var request = new UpdateBookDto
        {
            Title = "Updated Clean Code",
            SubTitle = "Updated Subtitle",
            AuthorId = 2
        };

        var result = await service.UpdateAsync(1, request);

        Assert.NotNull(result);
        Assert.Equal("Updated Clean Code", result.Title);
        Assert.Equal("Martin Fowler", result.Author.Name);
    }

    [Fact]
    public async Task UpdateAsync_NonexistentBook_ReturnsNull()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var request = new UpdateBookDto
        {
            Title = "Updated Book",
            AuthorId = 1
        };

        var result = await service.UpdateAsync(999, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_InvalidAuthor_ThrowsException()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var request = new UpdateBookDto
        {
            Title = "Updated Book",
            AuthorId = 999
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(1, request));
    }

    [Fact]
    public async Task DeleteAsync_ExistingBook_ReturnsTrue()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var result = await service.DeleteAsync(1);

        Assert.True(result);
        Assert.False(await context.Books.AnyAsync(
            b => b.BookId == 1));
    }

    [Fact]
    public async Task DeleteAsync_NonexistentBook_ReturnsFalse()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var result = await service.DeleteAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task SearchAsync_ByTitle_ReturnsMatchingBooks()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var filter = new BookSearchDto
        {
            Title = "clean",
            Page = 1,
            PageSize = 10
        };

        var result = await service.SearchAsync(filter);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task SearchAsync_ByAuthor_ReturnsMatchingBooks()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var filter = new BookSearchDto
        {
            Author = "fowler",
            Page = 1,
            PageSize = 10
        };

        var result = await service.SearchAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal("Refactoring", result.Items.First().Title);
    }

    [Fact]
    public async Task SearchAsync_CombinedFilters_ReturnsMatchingBook()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var filter = new BookSearchDto
        {
            Title = "architecture",
            Author = "robert",
            Page = 1,
            PageSize = 10
        };

        var result = await service.SearchAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal("Clean Architecture",
            result.Items.First().Title);
    }

    [Fact]
    public async Task SearchAsync_Pagination_ReturnsCorrectPage()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var filter = new BookSearchDto
        {
            Page = 2,
            PageSize = 1
        };

        var result = await service.SearchAsync(filter);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Single(result.Items);
        Assert.Equal("Clean Architecture",
            result.Items.First().Title);
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmptyResult()
    {
        var (connection, context, service) =
            await CreateTestEnvironmentAsync();

        await using var db = context;
        await using var conn = connection;

        var filter = new BookSearchDto
        {
            Title = "Nonexistent Title",
            Page = 1,
            PageSize = 10
        };

        var result = await service.SearchAsync(filter);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}
