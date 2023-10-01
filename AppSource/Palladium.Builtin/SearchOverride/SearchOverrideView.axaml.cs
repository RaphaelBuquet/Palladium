using Avalonia.ReactiveUI;
using Palladium.ExtensionFunctions.Lifecycle;

namespace Palladium.Builtin.SearchOverride;

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