using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Palladium.Controls;

public class ValidationMessage : TemplatedControl
{
	public static readonly StyledProperty<IEnumerable<object>?> ErrorsProperty = AvaloniaProperty.Register<ValidationMessage, IEnumerable<object>?>(
		nameof(Errors));

	/// <inheritdoc />
	public ValidationMessage()
	{
		if (Design.IsDesignMode)
		{
			Errors = new [] { "This field cannot be empty." };
		}
	}

	public IEnumerable<object>? Errors
	{
		get => GetValue(ErrorsProperty);
		set => SetValue(ErrorsProperty, value);
	}
}