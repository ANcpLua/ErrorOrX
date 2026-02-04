// EOE029: Asp.Versioning package not referenced
// ===============================================
// Asp.Versioning.Http package is not referenced but [ApiVersion] attributes are used.
// Install the package: dotnet add package Asp.Versioning.Http
//
// This demo file won't trigger EOE029 because the package IS referenced.
// The diagnostic appears when you use versioning attributes without the package.

// -------------------------------------------------------------------------
// HOW TO TRIGGER EOE029:
// -------------------------------------------------------------------------
// 1. Remove Asp.Versioning.Http from your .csproj
// 2. Define your own fake ApiVersionAttribute:
//
//    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
//    public class ApiVersionAttribute : Attribute
//    {
//        public ApiVersionAttribute(string version) { }
//    }
//
// 3. Use it on an endpoint class
// 4. The generator detects versioning intent but no real package

using Asp.Versioning;

namespace DiagnosticsDemos.Demos;

// -------------------------------------------------------------------------
// FIXED: With Asp.Versioning.Http package properly referenced
// -------------------------------------------------------------------------
[ApiVersion("1.0")]
public static class EOE029_ApiVersioningPackageNotReferenced
{
    [Get("/api/eoe029/items")]
    public static ErrorOr<string> GetItems()
    {
        return "items";
    }

    // -------------------------------------------------------------------------
    // To use API versioning, add the package:
    // -------------------------------------------------------------------------
    // dotnet add package Asp.Versioning.Http
    //
    // Then configure in Program.cs:
    //
    // builder.Services.AddApiVersioning(options =>
    // {
    //     options.DefaultApiVersion = new ApiVersion(1, 0);
    //     options.AssumeDefaultVersionWhenUnspecified = true;
    //     options.ReportApiVersions = true;
    //     options.ApiVersionReader = ApiVersionReader.Combine(
    //         new UrlSegmentApiVersionReader(),
    //         new HeaderApiVersionReader("X-Api-Version")
    //     );
    // });
}

// -------------------------------------------------------------------------
// TIP: Common API versioning setup
// -------------------------------------------------------------------------
//
// 1. Add NuGet package:
//    <PackageReference Include="Asp.Versioning.Http" Version="9.1.0" />
//
// 2. Configure services:
//    builder.Services.AddApiVersioning();
//
// 3. Add version attributes to endpoints:
//    [ApiVersion("1.0")]
//    public static class MyApi { ... }
//
// 4. Route pattern options:
//    /api/v{version:apiVersion}/items  (URL segment)
//    /api/items?api-version=1.0        (query string)
//    X-Api-Version: 1.0                (header)
