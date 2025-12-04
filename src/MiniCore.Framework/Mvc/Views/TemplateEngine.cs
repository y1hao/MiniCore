using System.Linq;
using System.Text;
using System.Text.Encodings.Web;

namespace MiniCore.Framework.Mvc.Views;

/// <summary>
/// Simple template engine that supports variable substitution, conditionals, and loops.
/// </summary>
public class TemplateEngine
{
    private readonly HtmlEncoder _htmlEncoder;

    public TemplateEngine()
    {
        _htmlEncoder = HtmlEncoder.Default;
    }

    /// <summary>
    /// Renders a template with the given model and view data.
    /// </summary>
    public string Render(string template, object? model, Dictionary<string, object>? viewData = null)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        var context = new TemplateContext
        {
            Model = model,
            ViewData = viewData ?? new Dictionary<string, object>(),
            HtmlEncoder = _htmlEncoder
        };

        return RenderTemplate(template, context);
    }

    private string RenderTemplate(string template, TemplateContext context)
    {
        var result = new StringBuilder();
        var index = 0;

        while (index < template.Length)
        {
            var openIndex = template.IndexOf("{{", index, StringComparison.Ordinal);
            if (openIndex == -1)
            {
                // No more template tags, append remaining text
                result.Append(template.Substring(index));
                break;
            }

            // Append text before the tag
            result.Append(template.Substring(index, openIndex - index));

            // Find closing tag
            var closeIndex = template.IndexOf("}}", openIndex + 2, StringComparison.Ordinal);
            if (closeIndex == -1)
            {
                // Malformed template, append remaining text
                result.Append(template.Substring(openIndex));
                break;
            }

            var tagContent = template.Substring(openIndex + 2, closeIndex - openIndex - 2).Trim();
            index = closeIndex + 2;

            // Process the tag
            var rendered = ProcessTag(tagContent, template, ref index, context);
            result.Append(rendered);
        }

        return result.ToString();
    }

    private string ProcessTag(string tagContent, string template, ref int index, TemplateContext context)
    {
        if (tagContent.StartsWith("#if "))
        {
            return ProcessIf(tagContent.Substring(4).Trim(), template, ref index, context);
        }
        else if (tagContent.StartsWith("/if"))
        {
            return string.Empty; // Closing tag, handled by ProcessIf
        }
        else if (tagContent.StartsWith("#each "))
        {
            return ProcessEach(tagContent.Substring(6).Trim(), template, ref index, context);
        }
        else if (tagContent.StartsWith("/each"))
        {
            return string.Empty; // Closing tag, handled by ProcessEach
        }
        else if (tagContent.StartsWith("else"))
        {
            return string.Empty; // Else tag, handled by parent if/else block
        }
        else
        {
            // Variable substitution
            return GetValue(tagContent, context);
        }
    }

    private string ProcessIf(string condition, string template, ref int index, TemplateContext context)
    {
        var conditionResult = EvaluateCondition(condition, context);
        var ifBlock = ExtractBlock(template, ref index, "{{#if", "{{/if}}", "{{else}}");
        
        if (conditionResult)
        {
            return RenderTemplate(ifBlock.TrueBlock, context);
        }
        else if (ifBlock.HasElse)
        {
            return RenderTemplate(ifBlock.FalseBlock, context);
        }
        
        return string.Empty;
    }

    private string ProcessEach(string collectionPath, string template, ref int index, TemplateContext context)
    {
        var collection = GetValueAsCollection(collectionPath, context);
        if (collection == null)
        {
            return string.Empty;
        }

        var loopBlock = ExtractBlock(template, ref index, "{{#each", "{{/each}}");
        var result = new StringBuilder();

        foreach (var item in collection)
        {
            var itemContext = new TemplateContext
            {
                Model = item,
                ViewData = context.ViewData,
                HtmlEncoder = context.HtmlEncoder,
                ParentContext = context
            };

            var rendered = RenderTemplate(loopBlock.Content, itemContext);
            result.Append(rendered);
        }

        return result.ToString();
    }

    private (string Content, bool HasElse, string TrueBlock, string FalseBlock) ExtractBlock(
        string template, 
        ref int index, 
        string openTag, 
        string closeTag, 
        string? elseTag = null)
    {
        var startIndex = index;
        var depth = 1;
        var elseIndex = -1;

        while (index < template.Length && depth > 0)
        {
            var nextOpen = template.IndexOf("{{", index, StringComparison.Ordinal);
            if (nextOpen == -1)
            {
                break;
            }

            var nextClose = template.IndexOf("}}", nextOpen + 2, StringComparison.Ordinal);
            if (nextClose == -1)
            {
                break;
            }

            var tagContent = template.Substring(nextOpen + 2, nextClose - nextOpen - 2).Trim();
            // closeTag is like "{{/if}}", so we need to extract just "/if" (remove "{{" and "}}")
            var closeTagPrefix = closeTag.Substring(2); // Remove "{{" -> "/if}}"
            var closeTagContent = closeTagPrefix.Substring(0, closeTagPrefix.Length - 2).Trim(); // Remove "}}" -> "/if"

            if (elseTag != null && tagContent == "else" && depth == 1 && elseIndex == -1)
            {
                elseIndex = nextOpen;
            }
            else if (tagContent.StartsWith(openTag.Substring(2))) // Remove "{{" from openTag
            {
                depth++;
            }
            else if (tagContent == closeTagContent || tagContent.StartsWith(closeTagContent + " ")) // Match closing tag exactly or with trailing space
            {
                depth--;
                if (depth == 0)
                {
                    var blockEnd = nextOpen;
                    var blockContent = template.Substring(startIndex, blockEnd - startIndex);

                    if (elseIndex != -1)
                    {
                        var trueBlock = template.Substring(startIndex, elseIndex - startIndex);
                        var falseBlock = template.Substring(elseIndex + 6, blockEnd - elseIndex - 6); // 6 = "{{else}}"
                        index = nextClose + 2;
                        return (blockContent, true, trueBlock, falseBlock);
                    }

                    index = nextClose + 2;
                    return (blockContent, false, blockContent, string.Empty);
                }
            }

            index = nextClose + 2;
        }

        // Block not properly closed, return what we have
        return (string.Empty, false, string.Empty, string.Empty);
    }

    private bool EvaluateCondition(string condition, TemplateContext context)
    {
        condition = condition.Trim();

        // Check if condition is a simple boolean value
        if (bool.TryParse(condition, out var boolValue))
        {
            return boolValue;
        }

        // Check if condition is a variable path
        var value = ResolvePath(condition, context);
        
        if (value == null)
        {
            return false;
        }

        // Convert to boolean
        if (value is bool boolVal)
        {
            return boolVal;
        }

        if (value is string str)
        {
            if (bool.TryParse(str, out var parsedBool))
            {
                return parsedBool;
            }
            // Non-empty string is truthy
            return !string.IsNullOrEmpty(str);
        }

        // Check if it's a collection
        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var count = enumerable.Cast<object>().Count();
            return count > 0;
        }

        // Non-null is truthy
        return value != null;
    }

    private string GetValue(string path, TemplateContext context, bool encode = true)
    {
        var value = ResolvePath(path, context);
        if (value == null)
        {
            return string.Empty;
        }

        var stringValue = value.ToString() ?? string.Empty;
        return encode ? _htmlEncoder.Encode(stringValue) : stringValue;
    }

    private object? ResolvePath(string path, TemplateContext context)
    {
        path = path.Trim();

        // Handle "this" keyword (for loops)
        if (path == "this" || path == ".")
        {
            return context.Model;
        }

        // Handle "this.PropertyName" (for loops)
        if (path.StartsWith("this.", StringComparison.OrdinalIgnoreCase))
        {
            var propertyPath = path.Substring(5); // Remove "this."
            return ResolvePropertyPath(context.Model, propertyPath);
        }

        // Handle direct "model" reference
        if (path.Equals("model", StringComparison.OrdinalIgnoreCase))
        {
            return context.Model;
        }

        // Handle ViewData access
        if (path.StartsWith("ViewData.", StringComparison.OrdinalIgnoreCase))
        {
            var key = path.Substring(9); // Remove "ViewData."
            if (context.ViewData.TryGetValue(key, out var value))
            {
                return value;
            }
            return null;
        }

        // Handle model property access
        if (path.StartsWith("model.", StringComparison.OrdinalIgnoreCase))
        {
            path = path.Substring(6); // Remove "model."
            return ResolvePropertyPath(context.Model, path);
        }

        // Try as direct model property or ViewData
        // But only if it doesn't start with "this." (which we already handled above)
        if (!path.StartsWith("this.", StringComparison.OrdinalIgnoreCase))
        {
            var directModel = ResolvePropertyPath(context.Model, path);
            if (directModel != null)
            {
                return directModel;
            }

            if (context.ViewData.TryGetValue(path, out var viewDataValue))
            {
                return viewDataValue;
            }
        }

        return null;
    }

    private object? ResolvePropertyPath(object? obj, string path)
    {
        if (obj == null || string.IsNullOrEmpty(path))
        {
            return null;
        }

        var parts = path.Split('.');
        var current = obj;

        foreach (var part in parts)
        {
            if (current == null)
            {
                return null;
            }

            // Handle array/collection index
            if (part.Contains('['))
            {
                var indexStart = part.IndexOf('[');
                var propertyName = part.Substring(0, indexStart);
                var indexEnd = part.IndexOf(']', indexStart);
                var indexStr = part.Substring(indexStart + 1, indexEnd - indexStart - 1);

                if (!string.IsNullOrEmpty(propertyName))
                {
                    current = GetPropertyValue(current, propertyName);
                }

                if (int.TryParse(indexStr, out var index))
                {
                    current = GetIndexValue(current, index);
                }
            }
            else
            {
                current = GetPropertyValue(current, part);
            }
        }

        return current;
    }

    private object? GetPropertyValue(object obj, string propertyName)
    {
        var type = obj.GetType();
        var property = type.GetProperty(propertyName, 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.IgnoreCase);

        if (property != null && property.CanRead)
        {
            return property.GetValue(obj);
        }

        // Try as field
        var field = type.GetField(propertyName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);

        if (field != null)
        {
            return field.GetValue(obj);
        }

        return null;
    }

    private object? GetIndexValue(object? obj, int index)
    {
        if (obj == null)
        {
            return null;
        }

        // Handle arrays
        if (obj is Array array && index >= 0 && index < array.Length)
        {
            return array.GetValue(index);
        }

        // Handle IList
        if (obj is System.Collections.IList list && index >= 0 && index < list.Count)
        {
            return list[index];
        }

        // Handle IEnumerable with index access via LINQ
        if (obj is System.Collections.IEnumerable enumerable)
        {
            var items = enumerable.Cast<object>().ToArray();
            if (index >= 0 && index < items.Length)
            {
                return items[index];
            }
        }

        return null;
    }

    private IEnumerable<object>? GetValueAsCollection(string path, TemplateContext context)
    {
        var value = ResolvePath(path, context);
        if (value == null)
        {
            return null;
        }

        if (value is IEnumerable<object> enumerable)
        {
            return enumerable;
        }

        // Handle non-generic IEnumerable
        if (value is System.Collections.IEnumerable nonGeneric)
        {
            return nonGeneric.Cast<object>().ToList();
        }

        return null;
    }

    private class TemplateContext
    {
        public object? Model { get; set; }
        public Dictionary<string, object> ViewData { get; set; } = new();
        public HtmlEncoder HtmlEncoder { get; set; } = HtmlEncoder.Default;
        public TemplateContext? ParentContext { get; set; }
    }
}

