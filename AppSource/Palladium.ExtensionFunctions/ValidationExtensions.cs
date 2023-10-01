using Avalonia.Controls;

namespace Palladium.ObservableExtensions;

public static class ValidationExtensions
{
	// perhaps this could be improved by generating an expression, thereby avoiding the need to create a property on the view?
	public static string? GetAttachedValidation(this Control control)
	{
		return control.GetValue(DataValidationErrors.ErrorsProperty)?.OfType<string>().FirstOrDefault();
	}

	// perhaps this could be improved by generating an expression, thereby avoiding the need to create a property on the view?
	public static void SetAttachedValidation(this Control control, string? value)
	{
		string[]? enumerable = string.IsNullOrEmpty(value) ? null : new [] { value };
		control.SetValue(DataValidationErrors.ErrorsProperty, enumerable);
	}
}