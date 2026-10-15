
using Bookstore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        BookstoreDbContext context,
        CancellationToken cancellationToken = default)
    {
        if (await context.Authors.AnyAsync(cancellationToken))
        {
            return;
        }

        var authors = new List<Author>
        {
            new()
            {
                Name = "Robert C. Martin",
                Books = new List<Book>
                {
                    new()
                    {
                        Title = "Clean Code",
                        SubTitle = "A Handbook of Agile Software Craftsmanship"
                    },
                    new()
                    {
                        Title = "Clean Architecture",
                        SubTitle = "A Craftsman's Guide to Software Structure and Design"
                    }
                }
            },
            new()
            {
                Name = "Martin Fowler",
                Books = new List<Book>
                {
                    new()
                    {
                        Title = "Refactoring",
                        SubTitle = "Improving the Design of Existing Code"
                    },
                    new()
                    {
                        Title = "Patterns of Enterprise Application Architecture"
                    }
                }
            },
            new()
            {
                Name = "Eric Evans",
                Books = new List<Book>
                {
                    new()
                    {
                        Title = "Domain-Driven Design",
                        SubTitle = "Tackling Complexity in the Heart of Software"
                    }
                }
            }
        };

        await context.Authors.AddRangeAsync(authors, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
