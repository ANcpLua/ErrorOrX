using System.ComponentModel.DataAnnotations;

namespace DiagnosticsDemos.Demos;

/// <summary>
///     EOE039: DataAnnotations validation uses reflection — Parameters with validation attributes
///     (e.g. [Required], [StringLength]) trigger Validator.TryValidateObject which uses reflection internally.
/// </summary>
/// <remarks>
///     This may cause trim warnings when publishing with Native AOT.
///     Consider using FluentValidation with source generators or manual validation.
///     Severity: Info (validation works, but generates trim warnings).
/// </remarks>
public static class EOE039_ValidationUsesReflection
{
    // -------------------------------------------------------------------------
    // TRIGGERS EOE039: Parameter with validation attributes
    // -------------------------------------------------------------------------
    // The generator detects [Required] attribute and warns that validation
    // uses reflection internally.
    //
    // [Post("/api/eoe039/create")]
    // public static ErrorOr<TodoItem> Create(CreateTodoRequest request)
    //     => new TodoItem(1, request.Title, request.Description);

    // -------------------------------------------------------------------------
    // TRIGGERS EOE039: Multiple validation attributes
    // -------------------------------------------------------------------------
    // [Post("/api/eoe039/register")]
    // public static ErrorOr<UserResponse> Register(RegisterRequest request)
    //     => new UserResponse(request.Email, request.Name);

    // -------------------------------------------------------------------------
    // NO WARNING: No validation attributes
    // -------------------------------------------------------------------------
    [Post("/api/eoe039/simple")]
    public static ErrorOr<TodoItem> CreateSimple(SimpleTodoRequest request)
    {
        return new TodoItem(1, request.Title, request.Description);
    }

    // -------------------------------------------------------------------------
    // ALTERNATIVE: Manual validation (AOT-safe)
    // -------------------------------------------------------------------------
    [Post("/api/eoe039/manual")]
    public static ErrorOr<TodoItem> CreateWithManualValidation(SimpleTodoRequest request)
    {
        // Manual validation - fully AOT compatible
        if (string.IsNullOrWhiteSpace(request.Title)) return Error.Validation("Title.Required", "Title is required");

        if (request.Title.Length > 100)
            return Error.Validation("Title.TooLong", "Title must be 100 characters or less");

        return new TodoItem(1, request.Title, request.Description);
    }
}

// Request with validation attributes (triggers EOE039)
public sealed record CreateTodoRequest(
    [Required] [StringLength(100)] string Title,
    string? Description);

// Request with multiple validation attributes (triggers EOE039)
public sealed record RegisterRequest(
    [Required] [EmailAddress] string Email,
    [Required]
    [StringLength(50, MinimumLength = 2)]
    string Name,
    [Required] [MinLength(8)] string Password);

// Simple request without validation (no warning)
public sealed record SimpleTodoRequest(string Title, string? Description);

public sealed record TodoItem(int Id, string Title, string? Description);

public sealed record UserResponse(string Email, string Name);

// -------------------------------------------------------------------------
// TIP: AOT-safe validation alternatives
// -------------------------------------------------------------------------
//
// 1. Manual validation in the handler (most AOT-friendly)
//
// 2. FluentValidation with source generators:
//    https://docs.fluentvalidation.net/en/latest/aspnet.html
//
// 3. Custom validation source generator that generates validation code
//    at compile time instead of using reflection
//
// 4. If you must use DataAnnotations, acknowledge the trim warnings
//    and test thoroughly with AOT publishing
