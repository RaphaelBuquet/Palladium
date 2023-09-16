using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;

namespace Palladium.ObservableExtensions.Lifecycle;

/// <summary>
///     <para>
///         Implement this on your view model to get callbacks when the View Model is attached and detached from a view.
///         This is useful to know when the view is first activated, and to know when the view is being disposed.
///     </para>
///     <para>
///         For this to work at runtime, your view (user control or window) needs to call
///         <see cref="LifecycleCallbacks.InstallLifecycleHandler{TObject}" /> in the constructor and implement
///         <see cref="IDisposable" />. Your application also needs to call dispose on your view where appropriate.
///     </para>
///     <para>
///         In most case, ReactiveUI's WhenActivated is preferable to this, because calling Dispose on the view is tricky
///         to get right. WhenActivated will automatically dispose on the Unloaded event, which is easier, but less
///         flexible as sometimes dispose should be called when the control isn't used anymore, not when it's unloaded.
///     </para>
/// </summary>
/// <remarks>
/// </remarks>
public interface ILifecycleAwareViewModel
{
	/// <summary>
	///     Simply implement this as an auto property that is auto initialized. This object is manipulated by the View.
	///     It holds the callbacks that are invoked by the View through the code in <see cref="LifecycleCallbacks" />.
	/// </summary>
	/// <example>
	///     You can implement this interface by pasting this snippet into your VM source code:
	///     <code>
	///   		LifecycleActivator ILifecycleAwareViewModel.Activator { get; } = new ();
	/// 	</code>
	/// </example>
	LifecycleActivator Activator { get; }
}

public class LifecycleActivator
{
	public readonly List<Action<CompositeDisposable>> WhenAttachedBlocks = new ();
	public readonly CompositeDisposable WhenDetached = new();

	public void RunWhenAttached()
	{
		foreach (var block in WhenAttachedBlocks)
		{
			block.Invoke(WhenDetached);
		}
		WhenAttachedBlocks.Clear();
	}

	public void RunWhenDetached()
	{
		WhenDetached.Dispose();
		WhenDetached.Clear();
	}
}

/// <summary>
///     Syntactic sugar.
/// </summary>
/// <seealso cref="ILifecycleAwareViewModel" />
public static class LifecycleCallbacks
{
	/// <summary>
	///     <para>
	///         WhenAttached allows you to register a callback that is invoked when a ViewModel is set on a View.
	///         With the CompositeDisposable you can also handle cleaning up your ViewModel when it is removed from the View
	///         or when the View is disposed.
	///     </para>
	///     <para>
	///         The View to call <see cref="InstallLifecycleHandler{TObject}" /> in its constructor for this to function.
	///     </para>
	/// </summary>
	/// <param name="viewModel"></param>
	/// <param name="block">
	///     Callback invoked when the View Model is set on the view. The Action parameter (usually called 'disposables') allows
	///     you to collate all the disposables to be cleaned up when the View is disposed.
	/// </param>
	/// <typeparam name="T"></typeparam>
	/// <remarks>
	///     Views using <see cref="InstallLifecycleHandler{TObject}" /> have to be manually disposed.
	/// </remarks>
	public static void WhenAttached<T>(this T viewModel, Action<CompositeDisposable> block) where T : ILifecycleAwareViewModel
	{
		viewModel.Activator.WhenAttachedBlocks.Add(block);
	}

	/// <summary>
	///     Add runtime calls to <see cref="WhenAttached{T}" /> for the <paramref name="avaloniaObject" />'s
	///     <see cref="ILifecycleAwareViewModel" />.
	///     The <paramref name="avaloniaObject" /> needs to set the DataContext to null in <see cref="IDisposable.Dispose" />.
	/// </summary>
	/// <example>
	///     <code>
	/// 	public partial class ExampleView : ReactiveUserControl&lt;ExampleViewModel&gt;, IDisposable
	/// 	{
	/// 		public ExampleView()
	/// 		{
	/// 			InitializeComponent();
	/// 			this.InstallLifecycleHandler();
	/// 		}
	/// 	
	/// 		public void Dispose()
	/// 		{
	/// 			DataContext = null;
	/// 		}
	/// 	}
	/// 	</code>
	/// </example>
	/// <param name="avaloniaObject">The target view object (user control, window).</param>
	/// <typeparam name="TObject">Type of the view (user control, window).</typeparam>
	public static void InstallLifecycleHandler<TObject>(this TObject avaloniaObject) where TObject : StyledElement, IDisposable
	{
		avaloniaObject
			.GetObservable(StyledElement.DataContextProperty)
			.Select(x => x as ILifecycleAwareViewModel) // note: don't use OfType as that removes null values
			.PairWithPrevious()
			.Subscribe(OnViewModelChanged);
	}

	private static void OnViewModelChanged((ILifecycleAwareViewModel? Previous, ILifecycleAwareViewModel? Current) args)
	{
		args.Previous?.Activator.RunWhenDetached();
		args.Current?.Activator.RunWhenAttached();
	}
}