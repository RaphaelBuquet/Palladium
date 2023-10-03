using System.Collections;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ReactiveUI;

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

		// this doesn't work in XAML for some reason so it needs to be implemented in C#.
		this.WhenAnyValue(x => x.Errors)
			.Select(x => x is IEnumerable enumerable && enumerable.OfType<object>().Any())
			.BindTo(this, x => x.IsVisible);
	}

	public IEnumerable<object>? Errors
	{
		get => GetValue(ErrorsProperty);
		set => SetValue(ErrorsProperty, value);
	}
}