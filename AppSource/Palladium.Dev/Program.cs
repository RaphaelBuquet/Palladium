using Palladium.Settings;

string settingsFilePath = Path.Combine(
	Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
	"Palladium",
	"Settings.xml");
var settingsService = new SettingsService(null, settingsFilePath);


Console.ReadLine();