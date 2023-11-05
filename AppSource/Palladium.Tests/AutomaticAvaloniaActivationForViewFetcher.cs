using System.Reactive.Linq;
using Avalonia;
using ReactiveUI;

namespace Palladium.Tests;

public class AutomaticAvaloniaActivationForViewFetcher : IActivationForViewFetcher
{
	/// <inheritdoc />
	public int GetAffinityForView(Type view)
	{
		return typeof(Visual).IsAssignableFrom(view) ? 10 : 0;
	}

	/// <inheritdoc />
	public IObservable<bool> GetActivationForView(IActivatableView view)
	{
		return Observable.Return(true);
	}
}