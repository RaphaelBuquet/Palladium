using System.Linq.Expressions;
using System.Reactive.Linq;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Formatters;
using ReactiveUI.Validation.Formatters.Abstractions;
using Splat;

namespace Palladium.ExtensionFunctions;

public static class ValidationExtensions
{
	public static string? GetAttachedError(this Control control)
	{
		return control.GetValue(DataValidationErrors.ErrorsProperty)?.OfType<string>().FirstOrDefault();
	}

	public static void SetAttachedError(this Control control, string? value)
	{
		string[]? enumerable = string.IsNullOrEmpty(value) ? null : new [] { value };
		control.SetValue(DataValidationErrors.ErrorsProperty, enumerable);
	}

	/// <param name="view">IViewFor instance.</param>
	/// <param name="viewModel">ViewModel instance. Can be null, used for generic type resolution.</param>
	/// <param name="viewModelProperty">ViewModel property.</param>
	/// <param name="viewControl">The control to add the attached errors to.</param>
	/// <param name="formatter">
	///     Validation formatter. Defaults to <see cref="SingleLineFormatter" />. In order to override the global
	///     default value, implement <see cref="IValidationTextFormatter{TOut}" /> and register an instance of
	///     IValidationTextFormatter&lt;string&gt; into Splat.Locator.
	/// </param>
	public static IDisposable BindValidation<TView, TViewModel, TViewModelProperty, TViewControl>(
		this TView view,
		TViewModel? viewModel,
		Expression<Func<TViewModel, TViewModelProperty>> viewModelProperty,
		TViewControl viewControl,
		IValidationTextFormatter<string>? formatter = null)
		where TView : IViewFor<TViewModel>
		where TViewModel : class, IReactiveObject, IValidatableViewModel
		where TViewControl : Control
	{
		if (view is null) throw new ArgumentNullException(nameof(view));
		if (viewModelProperty is null) throw new ArgumentNullException(nameof(viewModelProperty));
		if (viewControl is null) throw new ArgumentNullException(nameof(viewControl));

		formatter ??= Locator.Current.GetService<IValidationTextFormatter<string>>() ??
		              SingleLineFormatter.Default;

		var vcObs = view
			.WhenAnyValue(v => v.ViewModel)
			.Where(vm => vm is not null)
			.SelectMany(vm => vm!.ValidationContext.ObserveFor(viewModelProperty))
			.Select(
				states => states
					.Select(state => formatter.Format(state.Text))
					.FirstOrDefault(msg => !string.IsNullOrEmpty(msg)) ?? string.Empty);

		return vcObs.Subscribe(
			viewControl.SetAttachedError,
			ex => LogHost.Default.Error(ex, $"{viewControl} Binding received an Exception!"));
	}
}