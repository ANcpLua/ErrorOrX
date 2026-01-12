# Migration Guide: ErrorOr v1.x to v2.0.0

This guide helps you migrate from ErrorOr v1.x (two separate packages) to v2.0.0 (unified single package).

## Breaking Changes Summary

### Single Package Architecture

ErrorOr v2.0.0 consolidates the previous two-package structure into a single unified package:

| v1.x | v2.0.0 |
|------|--------|
| `ErrorOrX` | `ErrorOrX` |
| `ErrorOrX` | `ErrorOrX` |

### Namespace Simplification

All types are now under the `ErrorOrX` namespace. The `.Core` and `.Endpoints` namespace segments have been removed.

### Automatic AOT Support

Native AOT compilation is now enabled automatically. You no longer need to define `ERROROR_JSON` or configure JSON source generators manually.

---

## Step-by-Step Migration

### Step 1: Update Package References

Remove the old packages and add the new unified package.

**Before (csproj):**
```xml
<ItemGroup>
  <PackageReference Include="ErrorOrX" Version="1.x.x" />
  <PackageReference Include="ErrorOrX" Version="1.x.x" />
</ItemGroup>
```

**After (csproj):**
```xml
<ItemGroup>
  <PackageReference Include="ErrorOr" Version="2.0.0" />
</ItemGroup>
```

**Using CLI:**
```bash
dotnet remove package ErrorOrX
dotnet remove package ErrorOrX
dotnet add package ErrorOr --version 2.0.0
```

---

### Step 2: Update Using Statements

Replace the old namespaces with the new unified namespace.

**Find and Replace Table:**

| Find | Replace With |
|------|--------------|
| `using ErrorOrX.ErrorOr;` | `using ErrorOr;` |
| `using ErrorOrX.Errors;` | `using ErrorOr;` |
| `using ErrorOrX.Results;` | `using ErrorOr;` |
| `using ErrorOrX.Generated;` | `using ErrorOr.Generated;` |

**Regex for IDE Find/Replace:**

Use this regex pattern to match all old using statements at once:

```
Find:    using ErrorOr\.(Core\.(ErrorOr|Errors|Results)|Endpoints\.Generated);
Replace: using ErrorOr;
```

For the generated namespace specifically:

```
Find:    using ErrorOr\.Endpoints\.Generated;
Replace: using ErrorOr.Generated;
```

**Visual Studio:**
1. Press `Ctrl+Shift+H` (Find and Replace in Files)
2. Enable "Use Regular Expressions" (Alt+E)
3. Paste the regex patterns above
4. Click "Replace All"

**JetBrains Rider:**
1. Press `Ctrl+Shift+R` (Replace in Path)
2. Enable "Regex" checkbox
3. Paste the regex patterns above
4. Click "Replace All"

**VS Code:**
1. Press `Ctrl+Shift+H` (Search and Replace)
2. Click the regex icon (`.*`)
3. Paste the regex patterns above
4. Click "Replace All"

---

### Step 3: Remove ERROROR_JSON Define

If you previously used the `ERROROR_JSON` compiler define for JSON serialization support, remove it from your project file.

**Before:**
```xml
<PropertyGroup>
  <DefineConstants>ERROROR_JSON</DefineConstants>
</PropertyGroup>
```

**After:**
```xml
<!-- Remove the ERROROR_JSON define entirely -->
<!-- AOT and JSON support is now automatic -->
```

If `ERROROR_JSON` was part of a larger define list:

**Before:**
```xml
<DefineConstants>DEBUG;ERROROR_JSON;OTHER_DEFINE</DefineConstants>
```

**After:**
```xml
<DefineConstants>DEBUG;OTHER_DEFINE</DefineConstants>
```

---

### Step 4: Rebuild the Project

Perform a clean build to ensure all old artifacts are removed:

```bash
dotnet clean
dotnet build
```

If you encounter any build errors, check that:
- All namespace updates from Step 2 were applied
- No references to the old packages remain in any `.csproj` files
- The `ERROROR_JSON` define has been removed

---

### Step 5: Verify the Application

Run your application and tests to confirm everything works correctly:

```bash
dotnet test
dotnet run
```

Check that:
- All ErrorOr result handling works as expected
- API endpoints return correct error responses
- JSON serialization/deserialization functions properly

---

## FAQ

### Q: I get "namespace not found" errors after migration

**A:** Ensure you have replaced all old using statements. Run the regex find/replace from Step 2 across your entire solution. Also verify the new `ErrorOrX` package is installed by checking your `.csproj` file.

### Q: My JSON serialization stopped working

**A:** In v2.0.0, JSON serialization is automatic and uses source generators for AOT compatibility. If you had custom JSON converters, ensure they are still registered in your serialization options. The built-in converters should handle `ErrorOr<T>` types automatically.

### Q: Can I use v1.x and v2.0.0 in the same solution?

**A:** This is not recommended due to namespace conflicts. Migrate all projects in your solution to v2.0.0 at the same time.

### Q: Do I need to update my minimal API endpoint handlers?

**A:** The API surface remains the same. Your endpoint handlers should work without modification after updating the namespaces.

### Q: What happened to the source generators from ErrorOrX?

**A:** The source generators are now included in the main `ErrorOrX` package. Generated types are under the `ErrorOr.Generated` namespace instead of `ErrorOrX.Generated`.

### Q: I was using internal types from ErrorOrX. Are they still available?

**A:** Some internal implementation details may have changed. If you were relying on internal types, review the v2.0.0 API surface. The public API remains stable and backward-compatible.

### Q: How do I report issues with the migration?

**A:** Open an issue on the GitHub repository with details about the error, your project configuration, and the steps to reproduce the problem.
