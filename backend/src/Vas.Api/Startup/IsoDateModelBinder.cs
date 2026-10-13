using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Vas.Api.Startup;

/// <summary>
/// Reads a date only as yyyy-MM-dd. The default binder also accepts forms such as 06/01/2026,
/// which can mean two different days.
/// </summary>
public sealed class IsoDateModelBinder : IModelBinder
{
    public const string Format = "yyyy-MM-dd";

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);

        var text = value.FirstValue;
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.CompletedTask;
        }

        if (DateOnly.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"Use the date format {Format}.");
        }

        return Task.CompletedTask;
    }
}

public sealed class IsoDateModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.Metadata.UnderlyingOrModelType == typeof(DateOnly) ? new IsoDateModelBinder() : null;
}
