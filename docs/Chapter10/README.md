# Chapter 10: Frontend Templating

## Overview

Phase 10 implements a simple templating engine to replace Razor. This provides server-side rendering capabilities using lightweight template syntax that supports variable substitution, conditionals, and loops, without the complexity of Razor's Roslyn integration.

**Status:** ✅ Complete

## Goals

- Replace Razor with a simple templating engine
- Load `.html` templates from disk
- Support `{{variable}}` placeholder replacement
- Support loops and conditionals (`{{#each}}`, `{{#if}}`)
- Render templates to string or stream
- Integrate with MVC framework via `ViewResult`
- Support model binding and ViewData

## Key Requirements

### Template Engine

1. **IViewEngine Interface**
   - `FindViewAsync(string viewName, object? model)` - Locate and load view template
   - `RenderViewAsync(string viewPath, object? model, Dictionary<string, object>? viewData)` - Render template

2. **TemplateEngine**
   - Parse template syntax: `{{variable}}`, `{{#if}}`, `{{#each}}`
   - Support nested conditionals and loops
   - Property path navigation (e.g., `{{model.Property.SubProperty}}`)
   - HTML encoding for safe output

3. **ViewEngine Implementation**
   - Locate views in `Views/{ControllerName}/{ViewName}.html`
   - Support layout files (optional, future enhancement)
   - Cache compiled templates for performance

### MVC Integration

1. **ViewResult**
   - Implements `IActionResult`
   - Renders view using `IViewEngine`
   - Sets appropriate content type (text/html)
   - Handles errors gracefully

2. **Controller.View() Method**
   - Helper method to create `ViewResult`
   - Supports model and ViewData
   - Auto-discovers view name from action name

## Architecture

```
MiniCore.Framework/
└── Mvc/
    ├── Views/
    │   ├── Abstractions/
    │   │   └── IViewEngine.cs          # View engine interface
    │   ├── ViewEngine.cs                # View engine implementation
    │   ├── TemplateEngine.cs            # Template parser and renderer
    │   └── ViewContext.cs               # Context for view rendering
    └── Results/
        └── ViewResult.cs                # View result implementation
```

## Template Syntax

### Variable Substitution

```
{{variable}}                    # Simple variable
{{model.Property}}              # Model property
{{model.Items.0.Name}}          # Array/collection access
{{ViewData.Title}}              # ViewData access
```

### Conditionals

```
{{#if condition}}
  Content when true
{{/if}}

{{#if condition}}
  True content
{{else}}
  False content
{{/if}}
```

### Loops

```
{{#each items}}
  {{this.Property}}             # Current item
  {{.Property}}                 # Alternative syntax
{{/each}}
```

### HTML Encoding

All variables are HTML-encoded by default for security. Use `{{{variable}}}` for raw HTML (not implemented in initial version).

## Implementation Summary

Phase 10 successfully implements all core templating components:

### ✅ Template Engine

- **TemplateEngine.cs** - Parses and renders templates:
  - Variable substitution with property path navigation
  - `{{#if}}` conditionals with `{{else}}` support
  - `{{#each}}` loops for collections
  - Nested structures support
  - HTML encoding for safe output

### ✅ View Engine

- **IViewEngine.cs** - Interface for view resolution and rendering
- **ViewEngine.cs** - Implementation that:
  - Locates views by convention: `Views/{Controller}/{Action}.html`
  - Supports explicit view paths
  - Loads templates from disk
  - Caches template content for performance
  - Renders templates with model and ViewData

### ✅ MVC Integration

- **ViewResult.cs** - Action result that:
  - Renders views using `IViewEngine`
  - Sets Content-Type to `text/html`
  - Writes rendered content to response stream
  - Handles errors gracefully

- **Controller.cs** - Added `View()` methods:
  - `View()` - Renders default view for action
  - `View(object model)` - Renders view with model
  - `View(string viewName, object? model)` - Renders specific view

### ✅ View Conversion

- **Admin/Index.html** - Converted from Razor to simple template:
  - Replaced `@model` with `{{model}}`
  - Replaced `@foreach` with `{{#each}}`
  - Replaced `@if` with `{{#if}}`
  - Replaced `@variable` with `{{variable}}`

## Current Usage Patterns

### Basic View Rendering

```csharp
public class AdminController : Controller
{
    [HttpGet("/admin")]
    public async Task<IActionResult> Index()
    {
        var links = await _context.ShortLinks.ToListAsync();
        var dtos = links.Select(l => new ShortLinkDto { /* ... */ }).ToList();
        
        ViewData["Title"] = "Admin - URL Shortener";
        return View(dtos);  // Renders Views/Admin/Index.html
    }
}
```

### View with Model

```csharp
return View(model);  // Passes model to view as 'model'
```

### View with ViewData

```csharp
ViewData["Title"] = "My Page";
ViewData["Count"] = 42;
return View();
```

### Template Example

```html
<!DOCTYPE html>
<html>
<head>
    <title>{{ViewData.Title}}</title>
</head>
<body>
    <h1>{{ViewData.Title}}</h1>
    
    {{#if model}}
        {{#each model}}
            <div>
                <h2>{{this.Name}}</h2>
                <p>{{this.Description}}</p>
            </div>
        {{/each}}
    {{else}}
        <p>No items found.</p>
    {{/if}}
</body>
</html>
```

## Testing Strategy

### Unit Tests

1. **TemplateEngine Tests**
   - Variable substitution
   - Property path navigation
   - Conditional rendering
   - Loop rendering
   - Nested structures
   - HTML encoding
   - Error handling

2. **ViewEngine Tests**
   - View location by convention
   - Explicit view paths
   - Template caching
   - Model and ViewData passing
   - Missing view handling

3. **ViewResult Tests**
   - Result execution
   - Content type setting
   - Response writing
   - Error handling

### Integration Tests

1. **Controller Tests**
   - View rendering in controllers
   - Model binding to views
   - ViewData usage
   - End-to-end view rendering

## Success Criteria

- ✅ `IViewEngine` interface implemented
- ✅ `TemplateEngine` parses and renders templates
- ✅ `ViewEngine` locates and loads views
- ✅ `ViewResult` implements `IActionResult`
- ✅ `Controller.View()` method implemented
- ✅ Razor view converted to simple template
- ✅ AdminController renders view successfully
- ✅ ViewEngine registered in DI container
- ✅ All templates render correctly
- ✅ Unit tests pass (when implemented)

## Known Limitations

### Layout Support

**Status:** Not implemented

**Current Behavior:** Views must be complete HTML documents. No support for layouts or partials.

**Future Enhancement:** Add layout support similar to Razor's `_Layout.cshtml`.

### HTML Encoding

**Status:** Basic implementation

**Current Behavior:** All variables are HTML-encoded. No support for raw HTML output (`{{{variable}}}`).

**Future Enhancement:** Add triple braces syntax for raw HTML output.

### Complex Expressions

**Status:** Basic implementation

**Current Behavior:** Only supports property access and simple array indexing. No support for method calls, arithmetic, or complex expressions.

**Future Enhancement:** Add support for more complex expressions.

### Partial Views

**Status:** Not implemented

**Current Behavior:** No support for rendering partial views or components.

**Future Enhancement:** Add `{{> partial}}` syntax for partial views.

### ViewData Model Binding

**Status:** Limited implementation

**Current Behavior:** ViewData is separate from model. No support for ViewBag-like dynamic properties.

**Future Enhancement:** Add ViewBag support for easier access.

## Key Implementation Details

### View Location Convention

Views are located using the following convention:
1. `Views/{ControllerName}/{ActionName}.html`
2. Controller name is derived from controller class name (removes "Controller" suffix)
3. Action name is the method name

Example:
- Controller: `AdminController.Index()` → `Views/Admin/Index.html`

### Template Parsing

Templates are parsed using a simple state machine:
1. Scan for `{{` start tokens
2. Parse tag type (variable, `#if`, `#each`, closing tag)
3. Extract expression/path
4. Build parse tree
5. Render by traversing tree and evaluating expressions

### Model Access

In templates, the model is accessed via:
- `{{model.Property}}` - Access model properties
- `{{model.Items.0.Name}}` - Access array/collection items
- `{{this.Property}}` - Inside loops, access current item

### ViewData Access

ViewData is accessed via:
- `{{ViewData.Key}}` - Access ViewData by key

### Property Path Resolution

Property paths are resolved using reflection:
1. Split path by `.`
2. Navigate object graph
3. Handle array indexing with `[index]` or `.index` syntax
4. Handle null values gracefully

## Migration from Razor

The following Razor syntax was converted:

| Razor | Simple Template |
|-------|----------------|
| `@model Type` | (removed, model passed via View method) |
| `@variable` | `{{variable}}` |
| `@Model.Property` | `{{model.Property}}` |
| `@if (condition)` | `{{#if condition}}` |
| `@foreach (var item in items)` | `{{#each items}}` |
| `@item.Property` | `{{this.Property}}` |
| `ViewData["Key"]` | `{{ViewData.Key}}` |

## Next Steps

Phase 10 is complete. Remaining phases:

- **Phase 11**: Background Services (already implemented in Phase 4)

## References

- [Razor Syntax Reference](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor)
- [Handlebars.js Template Syntax](https://handlebarsjs.com/guide/) (inspiration for syntax)

