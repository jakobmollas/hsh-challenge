using System.IO;
using System.IO.Abstractions;
using System.Windows;

namespace WeaponsWatcher;

// ReSharper disable once UnusedMember.Global - used by the framework indirectly
public partial class MainWindow
{
	private const string Filename = "weapons.json";

	private readonly WeaponsFileMonitor _monitor;

	public WeaponsViewModel ViewModel { get; }

	public MainWindow()
	{
		ViewModel = new WeaponsViewModel();
		DataContext = ViewModel;

		InitializeComponent();

		string pathToMonitor = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Filename);
		_monitor = new WeaponsFileMonitor(new FileSystem(), pathToMonitor, new PeriodicTimer(TimeSpan.FromMilliseconds(250)));
		_monitor.WeaponsUpdated += FileMonitor_WeaponsUpdated;

		Closed += MainWindow_Closed;
	}

	private async void MainWindow_Closed(object? sender, EventArgs e)
	{
		await _monitor.DisposeAsync();
	}

	private void FileMonitor_WeaponsUpdated(object? sender, IEnumerable<Weapon> weapons)
	{
		// There is an obvious corner case here, what if the change event is triggered faster than the consuming code can process them?
		// We could flood the UI with updates in that case.
		// Monitoring files is tricky business - we could add some hysteresis where we wait a short while before triggering read, for example wait until a second has passed with no new events.
		// And a queue on top of that to handle events that pop in during an ongoing read - we do not want to miss updates just because we are currently reading the file.
		// Doing that would introduce more corner cases and room for optimizations etc.
		Dispatcher.Invoke(() => ViewModel.UpdateWeapons(weapons));
	}

	private void ClearContents_Click(object sender, RoutedEventArgs e)
	{
		ViewModel.ClearWeapons();
	}
}