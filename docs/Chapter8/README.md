# Chapter 8: MVC Framework

## Overview

Phase 8 implements a minimal MVC Framework to replace `Microsoft.AspNetCore.Mvc`. This provides controller discovery, action method invocation, model binding, and action result execution without any dependency on Microsoft's MVC implementation.

**Status:** ✅ Complete

## Goals

- Implement `IController` interface and `Controller` base class
- Implement `IActionResult` interface and common result types
- Implement model binding with `[FromBody]`, `[FromQuery]`, `[FromRoute]` attributes
- Implement controller discovery and action method discovery
- Implement action invocation with parameter binding
- Move MVC-related functionality from routing framework (Phase 6) to MVC framework
- Remove all Microsoft MVC dependencies

## Key Requirements

### Controller Abstractions

1. **IController Interface**
   - Defines contract for controllers
   - Provides access to `HttpContext`

2. **Controller Base Class**
   - `ControllerBase` - Base class for API controllers
   - `Controller` - Base class for controllers with view support (for Phase 9)
   - Helper methods: `Ok()`, `BadRequest()`, `NotFound()`, `NoContent()`, `Created()`, `Redirect()`

### Action Results

1. **IActionResult Interface**
   - `ExecuteResultAsync(ActionContext)` - Executes the result

2. **Result Implementations**
   - `OkResult` / `OkObjectResult` - 200 OK
   - `BadRequestResult` / `BadRequestObjectResult` - 400 Bad Request
   - `NotFoundResult` / `NotFoundObjectResult` - 404 Not Found
   - `NoContentResult` - 204 No Content
   - `CreatedResult` - 201 Created
   - `RedirectResult` - 302 Redirect

### Model Binding

1. **Binding Attributes**
   - `[FromBody]` - Bind from request body (JSON)
   - `[FromQuery]` - Bind from query string
   - `[FromRoute]` - Bind from route parameters

2. **Model Binder**
   - `IModelBinder` interface
   - `DefaultModelBinder` implementation
   - Supports primitive types, nullable types, and complex types (JSON)

### Controller Discovery

1. **IControllerDiscovery Interface**
   - `DiscoverControllers()` - Finds controllers in assemblies
   - `GetActionMethods()` - Gets action methods for a controller

2. **ControllerDiscovery Implementation**
   - Convention-based discovery (name ends with "Controller")
   - Attribute-based discovery (`[Controller]` attribute)
   - Action method discovery (excludes `[NonAction]` methods)
   - HTTP method detection from attributes (`[HttpGet]`, `[HttpPost]`, etc.)

### Action Invocation

1. **IActionInvoker Interface**
   - `InvokeAsync(ActionContext)` - Invokes the action

2. **ControllerActionInvoker Implementation**
   - Controller instantiation via DI
   - Parameter binding using model binder
   - Action method invocation (sync and async)
   - Result execution

## Architecture

```
MiniCore.Framework/
└── Mvc/
    ├── Abstractions/
    │   ├── IController.cs              # Controller interface
    │   ├── IActionResult.cs            # Action result interface
    │   ├── IActionInvoker.cs           # Action invoker interface
    │   ├── IControllerDiscovery.cs     # Controller discovery interface
    │   └── ActionContext.cs            # Action execution context
    ├── Controllers/
    │   └── Controller.cs                # Controller base classes
    ├── Results/
    │   ├── StatusCodes.cs              # HTTP status code constants
    │   ├── OkResult.cs                 # 200 OK result
    │   ├── OkObjectResult.cs           # 200 OK with content
    │   ├── BadRequestResult.cs         # 400 Bad Request
    │   ├── BadRequestObjectResult.cs   # 400 Bad Request with content
    │   ├── NotFoundResult.cs           # 404 Not Found
    │   ├── NotFoundObjectResult.cs     # 404 Not Found with content
    │   ├── NoContentResult.cs          # 204 No Content
    │   ├── CreatedResult.cs            # 201 Created
    │   └── RedirectResult.cs           # 302 Redirect
    ├── ModelBinding/
    │   ├── FromBodyAttribute.cs        # [FromBody] attribute
    │   ├── FromQueryAttribute.cs       # [FromQuery] attribute
    │   ├── FromRouteAttribute.cs       # [FromRoute] attribute
    │   ├── IModelBinder.cs              # Model binder interface
    │   ├── ModelBindingContext.cs      # Model binding context
    │   └── DefaultModelBinder.cs       # Default model binder
    ├── ControllerDiscovery.cs          # Controller discovery implementation
    └── ControllerActionInvoker.cs      # Action invoker implementation
```

## Implementation Summary

Phase 8 successfully implements all core MVC components:

### ✅ Controller Abstractions

- **IController.cs** - Interface for controllers
- **Controller.cs** - Base classes:
  - `ControllerBase` - For API controllers
  - `Controller` - For controllers with views (Phase 9)
  - Helper methods for common action results

### ✅ Action Results

- **IActionResult.cs** - Interface for action results
- **Result Types**:
  - `OkResult` / `OkObjectResult` - 200 OK responses
  - `BadRequestResult` / `BadRequestObjectResult` - 400 Bad Request
  - `NotFoundResult` / `NotFoundObjectResult` - 404 Not Found
  - `NoContentResult` - 204 No Content
  - `CreatedResult` - 201 Created with Location header
  - `RedirectResult` - 302 Redirect

### ✅ Model Binding

- **Binding Attributes**:
  - `[FromBody]` - Binds from JSON request body
  - `[FromQuery]` - Binds from query string parameters
  - `[FromRoute]` - Binds from route parameters

- **DefaultModelBinder**:
  - Binds primitive types (int, string, bool, etc.)
  - Binds nullable types
  - Binds complex types from JSON body
  - Supports default values

### ✅ Controller Discovery

- **IControllerDiscovery** - Interface for discovering controllers
- **ControllerDiscovery** - Implementation that:
  - Discovers controllers by convention (name ends with "Controller")
  - Discovers controllers by attribute (`[Controller]`)
  - Discovers action methods (excludes `[NonAction]`)
  - Detects HTTP methods from attributes
  - Extracts route templates

### ✅ Action Invocation

- **IActionInvoker** - Interface for invoking actions
- **ControllerActionInvoker** - Implementation that:
  - Creates controller instances via DI
  - Binds action parameters using model binder
  - Invokes action methods (sync and async)
  - Executes action results
  - Handles errors gracefully

### ✅ Integration with Routing

- **ControllerMapper** - Updated to use MVC framework:
  - Uses `IControllerDiscovery` for controller discovery
  - Uses `ControllerActionInvoker` for action invocation
  - Removed all Microsoft MVC bridging code
  - `MapFallbackToController()` now uses our MVC framework

## Current Usage Patterns

### Basic Controller

```csharp
using MiniCore.Framework.Mvc.Controllers;
using MiniCore.Framework.Mvc.Results;

[Route("api/links")]
public class ShortLinkController : ControllerBase
{
    [HttpGet]
    public OkObjectResult GetLinks()
    {
        var links = new[] { /* ... */ };
        return Ok(links);
    }

    [HttpPost]
    public CreatedResult CreateLink([FromBody] CreateLinkRequest request)
    {
        // Create link...
        return Created($"/api/links/{id}", link);
    }
}
```

### Model Binding

```csharp
[HttpGet("{id}")]
public OkObjectResult GetLink([FromRoute] int id)
{
    // id is bound from route parameter
}

[HttpGet]
public OkObjectResult Search([FromQuery] string query, [FromQuery] int page = 1)
{
    // query and page are bound from query string
}

[HttpPost]
public OkObjectResult Create([FromBody] CreateRequest request)
{
    // request is bound from JSON body
}
```

### Action Results

```csharp
// Return 200 OK
return Ok();
return Ok(data);

// Return 400 Bad Request
return BadRequest();
return BadRequest(error);

// Return 404 Not Found
return NotFound();
return NotFound(error);

// Return 204 No Content
return NoContent();

// Return 201 Created
return Created("/api/links/123", link);

// Return 302 Redirect
return Redirect("https://example.com");
```

## Testing Strategy

### Unit Tests

1. **Controller Tests**
   - Controller instantiation
   - HttpContext assignment
   - Helper method return types

2. **ActionResult Tests**
   - Status code setting
   - Header setting
   - Body serialization

3. **Model Binding Tests**
   - Route parameter binding
   - Query string binding
   - Request body binding (JSON)
   - Type conversion
   - Default values

4. **Controller Discovery Tests**
   - Controller discovery by convention
   - Controller discovery by attribute
   - Action method discovery
   - HTTP method detection

5. **Action Invocation Tests**
   - Parameter binding
   - Action method invocation
   - Result execution
   - Error handling

## Success Criteria

- ✅ `IController` interface implemented
- ✅ `Controller` base class with helper methods
- ✅ `IActionResult` interface and implementations
- ✅ Model binding with `[FromBody]`, `[FromQuery]`, `[FromRoute]`
- ✅ Controller discovery implementation
- ✅ Action invocation implementation
- ✅ ControllerMapper updated to use MVC framework
- ✅ All Microsoft MVC dependencies removed
- ✅ Unit tests pass
- ✅ Ready for Phase 9 (Mini ORM)

## Known Limitations

### Model Validation

**Status:** Not implemented

**Current Behavior:** Model binding succeeds even if the model is invalid.

**Future Enhancement:** Add model validation similar to ASP.NET Core's `ModelState`.

### Complex Model Binding

**Status:** Basic implementation

**Current Behavior:** Only supports JSON deserialization for complex types.

**Future Enhancement:** Support form data, XML, and other content types.

### Action Filters

**Status:** Not implemented

**Current Behavior:** No support for action filters (authorization, logging, etc.).

**Future Enhancement:** Add action filter pipeline similar to ASP.NET Core.

## Key Implementation Details

### Controller Discovery

Controllers are discovered using:
1. **Convention**: Types ending with "Controller"
2. **Attribute**: Types marked with `[Controller]` attribute
3. **Interface**: Types implementing `IController` or inheriting from `ControllerBase`

Action methods are discovered by:
1. Public instance methods
2. Excluding special methods (properties, events)
3. Excluding methods marked with `[NonAction]`

### Model Binding Priority

When binding parameters without attributes:
1. Try route data first
2. Try query string second
3. Try request body third (for complex types)
4. Use default value if available

When using attributes:
- `[FromRoute]` - Only route data
- `[FromQuery]` - Only query string
- `[FromBody]` - Only request body

### Action Result Execution

Action results are executed by:
1. Setting HTTP status code
2. Setting response headers (if needed)
3. Serializing response body (if needed)
4. Writing to response stream

## Migration from Phase 6

The following functionality was moved from Phase 6 (Routing) to Phase 8 (MVC):

- Controller discovery logic → `ControllerDiscovery`
- Action method discovery → `ControllerDiscovery.GetActionMethods()`
- Controller instantiation → `ControllerActionInvoker.CreateControllerInstance()`
- Parameter binding → `ControllerActionInvoker.BindParameterAsync()`
- Action invocation → `ControllerActionInvoker.InvokeAsync()`
- Result execution → `ControllerActionInvoker.ExecuteResultAsync()`

The routing framework (Phase 6) now focuses solely on:
- Route pattern matching
- Route registration
- Route parameter extraction
- Route data storage

## Next Steps

Phase 8 is complete. Next phases:

- **Phase 9**: Mini ORM / Data Integration (replace EF Core)
- **Phase 10**: Frontend Templating (replace Razor)
- **Phase 11**: Background Services (already implemented in Phase 4)

## References

- [ASP.NET Core MVC Controllers](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions)
- [Model Binding in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding)
- [Action Results in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions#action-return-types)

