# ErrorOrX.Samples.Diagnostics

Realistic-looking API code that deliberately triggers ErrorOrX diagnostics. This project is **in `ErrorOrX.slnx` but excluded from the solution build** (`<Build Solution="*|*" Project="false" />`) — the source-generator-reported errors halt compilation, which is the entire point. Open the solution in your IDE for live squiggles, or build the project explicitly:

```bash
dotnet build samples/ErrorOrX.Samples.Diagnostics/ErrorOrX.Samples.Diagnostics.csproj
```

**The build is expected to fail.** Each failure is a curated example of a real mistake a consumer would naturally make and the diagnostic that catches it.

## What's here — verified firing diagnostics

| File                       | Diagnostics                                                    | Why this is the natural place                                       |
|----------------------------|----------------------------------------------------------------|---------------------------------------------------------------------|
| `Apis/SearchApi.cs`        | **EOE020** route constraint mismatch, **EOE021** ambiguous binding on `GET`/`DELETE` + complex DTO, **EOE024** undocumented interface call | Search endpoints with filter DTOs and id-typed routes               |
| `Apis/BlogApi.cs`          | **EOE003** unbound route param `{slug}`, **EOE004** duplicate `GET /api/posts` | Typo'd parameter name; two API classes both mapping the same route  |
| `Apis/OrdersApi.cs`        | **EOE007** body type (`NewOrder`, `Order`) not in `JsonSerializerContext` | `Order`/`NewOrder` deliberately omitted from `AppJsonContext`       |
| `Apis/NotificationsApi.cs` | **EOE024** undocumented interface call                         | `INotificationService` returns `ErrorOr<>` without `[ProducesError]` |
| `Apis/UploadApi.cs`        | **EOE006** multiple body sources (`[FromBody]` + `Stream`)     | Upload endpoint conflating metadata body with raw stream            |

**Seven EOE diagnostics fire across five realistic API files.** That covers the cross-graph cleverness (EOE004 duplicate-route, EOE007 cross-file JSON-context discovery, EOE024 call-graph correctness) and the smart-binding inference (EOE020/EOE021) — the diagnostics that actually justify the library's existence over stock Minimal API.

## Why these were picked (and why others weren't)

Source generators have two paths to surface a diagnostic: the **`DiagnosticAnalyzer`** (per-symbol, lightweight, fast) and the **generator pipeline** (cross-file, needs full compilation). The interesting diagnostics here exercise both — `EOE003`/`EOE020` come from the analyzer, `EOE004`/`EOE007`/`EOE021`/`EOE024` come from the generator. Together they show the library does work no per-method analyzer could do on its own.

The other 29 descriptors are mostly:
- **dry compile-error territory** (`EOE001`/`EOE002`/`EOE005`/`EOE018`) — caught by the C# compiler shape rules anyway,
- **attribute-misuse minutiae** (`EOE010`–`EOE014`/`EOE015`–`EOE017`) — cataloging not showcasing,
- **API-versioning specific** (`EOE027`–`EOE031`) — niche unless you do versioning,
- or **subsets of the picks above** (`EOE026` is `EOE007`'s sibling case).

## Notes on the EOE034 family

**`EOE034` (DataAnnotations validation uses reflection)** fires on endpoints of the shape `[Post("/x")] static ErrorOr<Created> Submit([Required] [StringLength(200)] string title, [Range(1,5)] int priority)`. Dual-reported by both the analyzer (IDE-time feedback via `ErrorOrEndpointAnalyzer.BodyAndValidation.cs`) and the source generator (build-time output + snapshot coverage). Detection routes through `ErrorOrContext.HasValidationNeeds`, which catches both direct attribute attribution on the parameter and deep validation needs on the parameter's type (record properties, IValidatableObject).

The IDs `EOE034`–`EOE036` previously held AOT-hostile call-site checks (Activator.CreateInstance, Type.GetType, Expression.Compile, dynamic) which were retired in 3.x in favour of `ANcpLua.Analyzers`' AL0094/AL0095/AL0101/AL0102. v4.0.0 reclaims these IDs for the JSON-context diagnostics (was EOE039-EOE041); see `CHANGELOG.md` for the migration table.
