
using Bookstore.Application.DTOs.Books;
using Bookstore.Application.DTOs.Common;
using Bookstore.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [Authorize(Policy = "BookManage")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var book = await _bookService.GetByIdAsync(
            id, cancellationToken);

        if (book is null)
            return NotFound();

        return Ok(book);
    }

    [Authorize(Policy = "BookManage")]
    [HttpPost]
    public async Task<ActionResult<BookDto>> Create(
        [FromBody] CreateBookDto request,
        CancellationToken cancellationToken)
    {
        var book = await _bookService.CreateAsync(
            request, cancellationToken);

        if (book is null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid author",
                Detail = "The specified author does not exist."
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = book.BookId },
            book);
    }

    [Authorize(Policy = "BookManage")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateBookDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var book = await _bookService.UpdateAsync(
                id, request, cancellationToken);

            if (book is null)
                return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid author",
                Detail = ex.Message
            });
        }
    }

    [Authorize(Policy = "BookManage")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _bookService.DeleteAsync(
            id, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [Authorize(Policy = "BookSearch")]
    [HttpPost("search")]
    [ProducesResponseType(
        typeof(PagedResultDto<BookDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<BookDto>>> Search(
        [FromBody] BookSearchDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _bookService.SearchAsync(
            filter,
            cancellationToken);

        return Ok(result);
    }
}
