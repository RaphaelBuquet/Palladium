using Avalonia.Media;
using ReactiveUI;

namespace Palladium.BuiltinActions.Settings;

public class SettingsEntryViewModel : ReactiveObject
{
	private FontWeight titleFontWeight = FontWeight.Normal;

	public SettingsEntryViewModel(string titleText, string sectionText, object view)
	{
		TitleText = titleText;
		View = view;
		SectionText = sectionText;
	}

	public FontWeight TitleFontWeight
	{
		get => titleFontWeight;
		set => this.RaiseAndSetIfChanged(ref titleFontWeight, value);
	}

	public string TitleText { get ; }
	public string SectionText { get ; }
	public object View { get ; }
}