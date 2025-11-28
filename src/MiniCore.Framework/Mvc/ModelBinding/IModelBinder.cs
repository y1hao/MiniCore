namespace MiniCore.Framework.Mvc.ModelBinding;

/// <summary>
/// Defines the contract for model binding.
/// </summary>
public interface IModelBinder
{
    /// <summary>
    /// Attempts to bind a model.
    /// </summary>
    /// <param name="context">The model binding context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BindModelAsync(ModelBindingContext context);
}

