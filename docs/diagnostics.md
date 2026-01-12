# ErrorOr Analyzer Diagnostics

This document lists all diagnostics produced by the ErrorOr analyzers.

## Diagnostic Summary

| Code | Severity | Title |
|------|----------|-------|
| [EOE001](#eoe001) | Error | Invalid return type |
| [EOE003](#eoe003) | Error | Unbound route parameter |
| [EOE004](#eoe004) | Error | Duplicate route |
| [EOE006](#eoe006) | Error | Multiple body parameters |
| [EOE007](#eoe007) | Warning | Missing JSON serializable |
| [EOE008](#eoe008) | Warning | Undocumented custom error |
| [EOE026](#eoe026) | Warning | Custom error without annotation |
| [EOE033](#eoe033) | Error | Interface error mismatch |

---

## EOE001

**Severity:** Error

**Title:** Invalid return type

**Message:** Method must return ErrorOr<T>, Task<ErrorOr<T>>, or ValueTask<ErrorOr<T>>

### Why This Occurs

This error occurs when a method decorated with ErrorOr endpoint attributes does not return one of the supported ErrorOr types. The analyzer requires methods to use the ErrorOr pattern for proper error handling and response generation.

### How to Fix

Change the method return type to one of the supported types.

**Incorrect:**

```csharp
[HttpGet("/users/{id}")]
public User GetUser(int id)  // EOE001: Invalid return type
{
    return _userService.GetById(id);
}
```

**Correct:**

```csharp
[HttpGet("/users/{id}")]
public ErrorOr<User> GetUser(int id)
{
    return _userService.GetById(id);
}

// Or for async methods:
[HttpGet("/users/{id}")]
public async Task<ErrorOr<User>> GetUserAsync(int id)
{
    return await _userService.GetByIdAsync(id);
}

// Or with ValueTask:
[HttpGet("/users/{id}")]
public async ValueTask<ErrorOr<User>> GetUserAsync(int id)
{
    return await _userService.GetByIdAsync(id);
}
```

---

## EOE003

**Severity:** Error

**Title:** Unbound route parameter

**Message:** Route parameter '{name}' is not bound to any method parameter

### Why This Occurs

This error occurs when a route template contains a parameter placeholder that does not have a corresponding method parameter. Every route parameter must be bound to a method parameter for the request to be processed correctly.

### How to Fix

Add a method parameter that matches the route parameter name, or remove the unused route parameter.

**Incorrect:**

```csharp
[HttpGet("/users/{userId}/orders/{orderId}")]
public ErrorOr<Order> GetOrder(int userId)  // EOE003: Route parameter 'orderId' is not bound
{
    return _orderService.GetOrder(userId);
}
```

**Correct:**

```csharp
[HttpGet("/users/{userId}/orders/{orderId}")]
public ErrorOr<Order> GetOrder(int userId, int orderId)
{
    return _orderService.GetOrder(userId, orderId);
}

// Or use [FromRoute] with a different parameter name:
[HttpGet("/users/{userId}/orders/{orderId}")]
public ErrorOr<Order> GetOrder(int userId, [FromRoute(Name = "orderId")] int id)
{
    return _orderService.GetOrder(userId, id);
}
```

---

## EOE004

**Severity:** Error

**Title:** Duplicate route

**Message:** Route '{pattern}' with method '{httpMethod}' is already defined

### Why This Occurs

This error occurs when two or more endpoint methods define the same route pattern with the same HTTP method. Each route and HTTP method combination must be unique to avoid ambiguous routing.

### How to Fix

Change one of the route patterns to make them unique, or use different HTTP methods.

**Incorrect:**

```csharp
[HttpGet("/users/{id}")]
public ErrorOr<User> GetUser(int id)
{
    return _userService.GetById(id);
}

[HttpGet("/users/{id}")]  // EOE004: Duplicate route '/users/{id}' with method 'GET'
public ErrorOr<UserDetails> GetUserDetails(int id)
{
    return _userService.GetDetailsById(id);
}
```

**Correct:**

```csharp
[HttpGet("/users/{id}")]
public ErrorOr<User> GetUser(int id)
{
    return _userService.GetById(id);
}

[HttpGet("/users/{id}/details")]  // Different route pattern
public ErrorOr<UserDetails> GetUserDetails(int id)
{
    return _userService.GetDetailsById(id);
}
```

---

## EOE006

**Severity:** Error

**Title:** Multiple body parameters

**Message:** Only one [FromBody] parameter is allowed

### Why This Occurs

This error occurs when a method has more than one parameter decorated with `[FromBody]`. HTTP requests can only have one request body, so only one parameter can be bound from it.

### How to Fix

Combine the body parameters into a single class, or use different binding sources for other parameters.

**Incorrect:**

```csharp
[HttpPost("/orders")]
public ErrorOr<Order> CreateOrder(
    [FromBody] OrderHeader header,   // EOE006: Multiple body parameters
    [FromBody] List<OrderItem> items)
{
    return _orderService.Create(header, items);
}
```

**Correct:**

```csharp
// Option 1: Combine into a single request class
public class CreateOrderRequest
{
    public OrderHeader Header { get; set; }
    public List<OrderItem> Items { get; set; }
}

[HttpPost("/orders")]
public ErrorOr<Order> CreateOrder([FromBody] CreateOrderRequest request)
{
    return _orderService.Create(request.Header, request.Items);
}

// Option 2: Use different binding sources
[HttpPost("/orders/{headerId}")]
public ErrorOr<Order> CreateOrder(
    [FromRoute] int headerId,
    [FromBody] List<OrderItem> items)
{
    var header = _orderService.GetHeader(headerId);
    return _orderService.Create(header, items);
}
```

---

## EOE007

**Severity:** Warning

**Title:** Missing JSON serializable

**Message:** Type '{type}' used by ErrorOr endpoints is not in any [JsonSerializable] context

### Why This Occurs

This warning occurs when using Native AOT or source-generated JSON serialization, and a type used in ErrorOr endpoints is not included in any `[JsonSerializable]` context. Without proper serialization context, the type cannot be serialized or deserialized at runtime in AOT scenarios.

### How to Fix

Add the type to a `JsonSerializerContext` with the `[JsonSerializable]` attribute.

**Incorrect:**

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[HttpGet("/users/{id}")]
public ErrorOr<User> GetUser(int id)  // EOE007: Type 'User' is not in any [JsonSerializable] context
{
    return _userService.GetById(id);
}

[JsonSerializable(typeof(Order))]  // User is missing
public partial class AppJsonContext : JsonSerializerContext { }
```

**Correct:**

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[HttpGet("/users/{id}")]
public ErrorOr<User> GetUser(int id)
{
    return _userService.GetById(id);
}

[JsonSerializable(typeof(User))]   // User is now included
[JsonSerializable(typeof(Order))]
public partial class AppJsonContext : JsonSerializerContext { }
```

---

## EOE008

**Severity:** Warning

**Title:** Undocumented custom error

**Message:** Error.Custom() with status {code} should have [ProducesError({code})]

### Why This Occurs

This warning occurs when a method returns an `Error.Custom()` with a specific HTTP status code, but that status code is not documented using the `[ProducesError]` attribute. Documenting error responses improves API documentation and helps API consumers understand possible error scenarios.

### How to Fix

Add a `[ProducesError]` attribute for each custom error status code the method can return.

**Incorrect:**

```csharp
[HttpGet("/users/{id}")]
public ErrorOr<User> GetUser(int id)  // EOE008: Error.Custom() with status 403 should have [ProducesError(403)]
{
    if (!_authService.HasAccess(id))
        return Error.Custom(403, "Access.Denied", "You do not have access to this user");

    return _userService.GetById(id);
}
```

**Correct:**

```csharp
[HttpGet("/users/{id}")]
[ProducesError(403, "Access denied")]
public ErrorOr<User> GetUser(int id)
{
    if (!_authService.HasAccess(id))
        return Error.Custom(403, "Access.Denied", "You do not have access to this user");

    return _userService.GetById(id);
}
```

---

## EOE026

**Severity:** Warning

**Title:** Custom error without annotation

**Message:** Using Error.Custom() without [ProducesError]

### Why This Occurs

This warning is similar to EOE008 but is more general. It triggers when any `Error.Custom()` call is detected in a method without corresponding `[ProducesError]` documentation, regardless of whether the analyzer can determine the specific status code.

### How to Fix

Add `[ProducesError]` attributes to document all custom errors that the method can return.

**Incorrect:**

```csharp
[HttpPost("/payments")]
public ErrorOr<Payment> ProcessPayment([FromBody] PaymentRequest request)
{
    // EOE026: Using Error.Custom() without [ProducesError]
    if (request.Amount <= 0)
        return Error.Custom(400, "Payment.InvalidAmount", "Amount must be positive");

    if (!_paymentGateway.IsAvailable())
        return Error.Custom(503, "Payment.ServiceUnavailable", "Payment service is down");

    return _paymentService.Process(request);
}
```

**Correct:**

```csharp
[HttpPost("/payments")]
[ProducesError(400, "Invalid payment amount")]
[ProducesError(503, "Payment service unavailable")]
public ErrorOr<Payment> ProcessPayment([FromBody] PaymentRequest request)
{
    if (request.Amount <= 0)
        return Error.Custom(400, "Payment.InvalidAmount", "Amount must be positive");

    if (!_paymentGateway.IsAvailable())
        return Error.Custom(503, "Payment.ServiceUnavailable", "Payment service is down");

    return _paymentService.Process(request);
}
```

---

## EOE033

**Severity:** Error

**Title:** Interface error mismatch

**Message:** [ReturnsError] on interface doesn't match implementation

### Why This Occurs

This error occurs when an interface method is decorated with `[ReturnsError]` attributes that specify certain error types, but the implementing class method does not have matching attributes. This ensures that error contracts defined in interfaces are properly implemented.

### How to Fix

Add matching `[ReturnsError]` attributes to the implementation method, or update the interface to match the implementation.

**Incorrect:**

```csharp
public interface IUserService
{
    [ReturnsError(typeof(NotFoundError))]
    [ReturnsError(typeof(ValidationError))]
    ErrorOr<User> GetUser(int id);
}

public class UserService : IUserService
{
    // EOE033: [ReturnsError] on interface doesn't match implementation
    [ReturnsError(typeof(NotFoundError))]  // Missing ValidationError
    public ErrorOr<User> GetUser(int id)
    {
        // ...
    }
}
```

**Correct:**

```csharp
public interface IUserService
{
    [ReturnsError(typeof(NotFoundError))]
    [ReturnsError(typeof(ValidationError))]
    ErrorOr<User> GetUser(int id);
}

public class UserService : IUserService
{
    [ReturnsError(typeof(NotFoundError))]
    [ReturnsError(typeof(ValidationError))]  // Now matches interface
    public ErrorOr<User> GetUser(int id)
    {
        if (id <= 0)
            return new ValidationError("Invalid user ID");

        var user = _repository.Find(id);
        if (user is null)
            return new NotFoundError($"User {id} not found");

        return user;
    }
}
```
