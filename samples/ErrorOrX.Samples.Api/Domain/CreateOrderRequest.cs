using System.ComponentModel.DataAnnotations;

namespace ErrorOrX.Samples.Api.Domain;

public sealed record CreateOrderRequest(
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string CustomerName,
    [Required] [EmailAddress] string Email,
    // NOTE: [MinLength]/[MaxLength]/[Length] carry a [RequiresUnreferencedCode] constructor (they reflect
    // for a 'Count' property on non-ICollection types), so they trip IL2026 under PublishAot. For Native
    // AOT, validate collection size in the handler (ErrorOr.Validation) instead; [Required] is AOT-clean.
    [Required] IReadOnlyList<OrderItem> Items);

public sealed record OrderItem(
    [Required] [StringLength(100)] string ProductName,
    [Range(1, 10000)] int Quantity,
    [Range(0.01, 999999.99)] decimal UnitPrice);

public sealed record UpdateOrderRequest(
    [StringLength(200)] string? CustomerName,
    [EmailAddress] string? Email);

public sealed record Order(
    Guid Id,
    string CustomerName,
    string Email,
    IReadOnlyList<OrderItem> Items,
    decimal TotalAmount,
    DateTimeOffset CreatedAt);
