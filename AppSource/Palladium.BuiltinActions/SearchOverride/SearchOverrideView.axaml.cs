using Avalonia.ReactiveUI;
using Palladium.ObservableExtensions.Lifecycle;

namespace Palladium.BuiltinActions.SearchOverride;

public partial class SearchOverrideView : ReactiveUserControl<SearchOverrideViewModel>, IDisposable
{
	public SearchOverrideView()
	{
		InitializeComponent();
		this.InstallLifecycleHandler();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		DataContext = null;
	}
}