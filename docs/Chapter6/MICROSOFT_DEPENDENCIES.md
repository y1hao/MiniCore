# Microsoft Dependencies in Routing Framework

## Overview

This document lists all remaining Microsoft dependencies in the Routing framework implementation (Chapter 6). These dependencies are used as bridges to work with Microsoft.AspNetCore.Mvc controllers until we implement our own MVC framework.

## Remaining Dependencies

### 1. Microsoft.AspNetCore.Http Namespace

**Location:** `ControllerMapper.cs` - `GetMicrosoftHttpContext()` method

**Dependencies:**
- `Microsoft.AspNetCore.Http.DefaultHttpContext` - Used to create HttpContext adapter
- `Microsoft.AspNetCore.Http.PathString` - Used for path representation
- `Microsoft.AspNetCore.Http.QueryString` - Used for query string representation

**Purpose:** Bridge our `HttpContext` with Microsoft's `HttpContext` so Microsoft MVC controllers can work.

**When to Remove:** Phase 8+ when we implement our own MVC framework and controllers use our `HttpContext` directly.

**Lines:** 389, 520, 530

---

### 2. Microsoft.AspNetCore.Mvc Namespace

**Location:** `ControllerMapper.cs` - `CreateControllerHandler()` method

**Dependencies:**
- `Microsoft.AspNetCore.Mvc.ControllerBase` - Base class for controllers
- `Microsoft.AspNetCore.Mvc.ControllerContext` - Controller execution context
- `Microsoft.AspNetCore.Mvc.IActionResult` - Action result interface
- `Microsoft.AspNetCore.Mvc.ActionContext` - Action execution context

**Purpose:** 
- Set up controller context for Microsoft controllers
- Execute `ActionResult` returned by controller methods

**When to Remove:** Phase 8+ when we implement our own MVC framework with our own controller base classes and result types.

**Lines:** 208, 211, 302, 305

---

### 3. Microsoft.AspNetCore.Routing Namespace

**Location:** `ControllerMapper.cs` - `GetMicrosoftHttpContext()` method

**Dependencies:**
- `Microsoft.AspNetCore.Routing.RouteData` - Route data structure

**Purpose:** Copy route data to Microsoft's `RouteData` format for compatibility.

**When to Remove:** Phase 8+ when controllers use our own route data directly.

**Lines:** 477

---

### 4. Microsoft.Extensions.Primitives Namespace

**Location:** `ControllerMapper.cs` - `CreateStringValues()` method

**Dependencies:**
- `Microsoft.Extensions.Primitives.StringValues` - String values collection

**Purpose:** Convert header values to Microsoft's `StringValues` format for HttpContext adapter.

**When to Remove:** Phase 8+ when we no longer need the HttpContext bridge.

**Lines:** 540

---

## Summary by Category

### Controller Execution (Phase 8+)
- `Microsoft.AspNetCore.Mvc.ControllerBase`
- `Microsoft.AspNetCore.Mvc.ControllerContext`
- `Microsoft.AspNetCore.Mvc.IActionResult`
- `Microsoft.AspNetCore.Mvc.ActionContext`

### HttpContext Bridging (Phase 8+)
- `Microsoft.AspNetCore.Http.DefaultHttpContext`
- `Microsoft.AspNetCore.Http.PathString`
- `Microsoft.AspNetCore.Http.QueryString`
- `Microsoft.Extensions.Primitives.StringValues`
- `Microsoft.AspNetCore.Routing.RouteData`

## Implementation Notes

All Microsoft dependencies are accessed via **reflection** to avoid compile-time dependencies. This allows:
- `MiniCore.Framework` to compile without Microsoft.AspNetCore packages
- Runtime bridging when Microsoft packages are available (in `MiniCore.Web`)
- Clean separation between our framework and Microsoft's MVC

## Migration Path

When implementing our own MVC framework (Phase 8+):

1. **Create our own controller base classes**
   - Replace `ControllerBase` with our own base class
   - Replace `ControllerContext` with our own context

2. **Create our own result types**
   - Replace `IActionResult` with our own result interface
   - Implement result execution pipeline

3. **Remove HttpContext bridging**
   - Controllers use our `HttpContext` directly
   - Remove `GetMicrosoftHttpContext()` method
   - Remove all Microsoft.Http type conversions

4. **Update controller execution**
   - Direct invocation without Microsoft abstractions
   - Use our own result execution pipeline

## Files Affected

- `ControllerMapper.cs` - Contains all Microsoft dependencies (via reflection)

## Files Without Microsoft Dependencies

✅ **RouteMatcher.cs** - Pure routing logic, no Microsoft dependencies  
✅ **RouteRegistry.cs** - Route storage and matching, no Microsoft dependencies  
✅ **EndpointRouteBuilder.cs** - Route builder, no Microsoft dependencies  
✅ **EndpointRouteBuilderExtensions.cs** - Extension methods, no Microsoft dependencies  
✅ **All Attributes** - Our own implementations, no Microsoft dependencies

