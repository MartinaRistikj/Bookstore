
using Bookstore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Infrastructure.Data.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(b => b.BookId);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.SubTitle)
            .IsRequired(false);

        builder.HasIndex(b => b.AuthorId);
    }
}
