using Avalonia.ReactiveUI;
using Palladium.ObservableExtensions.Lifecycle;

namespace Palladium.Builtin.ImmersiveGame;

public partial class ImmersiveGameControl : ReactiveUserControl<ImmersiveGameViewModel>, IDisposable
{
	public ImmersiveGameControl()
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