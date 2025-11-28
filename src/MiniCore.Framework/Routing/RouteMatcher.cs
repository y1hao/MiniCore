using MiniCore.Framework.Routing.Abstractions;

namespace MiniCore.Framework.Routing;

/// <summary>
/// Default implementation of <see cref="IRouteMatcher"/>.
/// </summary>
public class RouteMatcher : IRouteMatcher
{
    /// <inheritdoc />
    public bool TryMatch(string pattern, string path, out RouteData? routeData)
    {
        routeData = null;

        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(path))
        {
            return false;
        }

        // Normalize paths
        pattern = NormalizePath(pattern);
        path = NormalizePath(path);

        // Handle catch-all patterns (e.g., "{*path}")
        if (pattern.EndsWith("{*path}", StringComparison.OrdinalIgnoreCase))
        {
            var basePattern = pattern.Substring(0, pattern.Length - 7).TrimEnd('/');
            if (path.StartsWith(basePattern, StringComparison.OrdinalIgnoreCase))
            {
                routeData = new RouteData();
                var remainingPath = path.Substring(basePattern.Length).TrimStart('/');
                routeData.Values["path"] = remainingPath;
                return true;
            }
            return false;
        }

        // Split pattern and path into segments
        var patternSegments = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Must have same number of segments (unless pattern has catch-all)
        if (patternSegments.Length != pathSegments.Length)
        {
            return false;
        }

        routeData = new RouteData();

        // Match each segment
        for (int i = 0; i < patternSegments.Length; i++)
        {
            var patternSegment = patternSegments[i];
            var pathSegment = pathSegments[i];

            if (IsParameter(patternSegment))
            {
                // Extract parameter name and value
                var paramName = ExtractParameterName(patternSegment);
                routeData.Values[paramName] = pathSegment;
            }
            else
            {
                // Literal segment must match exactly
                if (!string.Equals(patternSegment, pathSegment, StringComparison.OrdinalIgnoreCase))
                {
                    routeData = null;
                    return false;
                }
            }
        }

        return true;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "/";
        }

        // Remove leading/trailing slashes and normalize
        path = path.Trim('/');
        return "/" + path;
    }

    private static bool IsParameter(string segment)
    {
        return segment.StartsWith("{", StringComparison.Ordinal) && 
               segment.EndsWith("}", StringComparison.Ordinal);
    }

    private static string ExtractParameterName(string segment)
    {
        // Remove { and }
        return segment.TrimStart('{').TrimEnd('}');
    }
}

