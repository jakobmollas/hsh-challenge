using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeaponsWatcher;

// A different approach to using a PeriodicTimer (which is async) could be to use Dispatcher.BeginInvoke and run short diff checks on the target file.
// That would simplify the solution somewhat (no need to marshall calls to the UI thread, no need for separate threads) but it could also remove some separation of concerns.
// I am also not sure how to schedule an update to not run immediately, but in x milliseconds or similar.

internal sealed class WeaponsFileMonitor : IAsyncDisposable
{
	private static readonly JsonSerializerOptions _jsonOptions = new() { Converters = { new JsonStringEnumConverter() } };

	private readonly CancellationTokenSource _cts = new();
	private readonly Task _monitorTask;
	private readonly string _pathToMonitor;
	private readonly PeriodicTimer _timer;
	private DateTime _lastWriteTime;

	public event EventHandler<IEnumerable<Weapon>>? WeaponsUpdated;

	/// <summary>
	/// Create a new <see cref="WeaponsFileMonitor"/> that continuously monitors <paramref name="pathToMonitor"/>
	/// for changes (using LastWriteTimeUtc) with a delay of <paramref name="interval"/>.
	/// When the file is modified, the <see cref="WeaponsUpdated"/> event will be fired with included deserialized file contents.
	/// Any errors will be suppressed and will not result in a change event. 
	/// Dispose the instance to terminate the monitoring operation.
	/// </summary>
	/// <remarks>
	/// Cancellation/dispose could result in <see cref="OperationCanceledException"/>.
	/// </remarks>
	public WeaponsFileMonitor(string pathToMonitor, TimeSpan interval)
	{
		if (string.IsNullOrWhiteSpace(pathToMonitor))
			throw new ArgumentException("Value cannot be null or whitespace.", nameof(pathToMonitor));

		if (interval <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(interval), $"{nameof(interval)} must be greater than 0.");

		_pathToMonitor = pathToMonitor;

		_monitorTask = Task.Run(async () => await RunAsync(_cts.Token));
		_timer = new PeriodicTimer(interval);
	}

	public async ValueTask DisposeAsync()
	{
		_cts.Cancel();
		await _monitorTask;
	}

	private async Task RunAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			// Dispose/token cancel does not result in an OperationCanceledException
			// so we do not need to do a try/catch
			bool result = await _timer.WaitForNextTickAsync(ct);
			if (!result)
				return;

			await CheckForUpdatesAsync(ct);
		}
	}

	private async Task CheckForUpdatesAsync(CancellationToken ct)
	{
		try
		{
			// No listeners or nothing to read? Well, no point in working hard in that case.
			// By checking for subscribers we also handle an edge case where a consumer creates a monitor
			// that starts and processes a file BEFORE the consumer has time to subscribe to the change event,
			// which would lead to a lost initial update.
			if (WeaponsUpdated == null || !File.Exists(_pathToMonitor))
				return;

			var writeTime = File.GetLastWriteTimeUtc(_pathToMonitor);
			if (writeTime == _lastWriteTime)
				return;

			_lastWriteTime = writeTime;

			var weapons = await ReadWeaponsAsync(ct);
			WeaponsUpdated?.Invoke(this, weapons);
		}
		catch (Exception)
		{
			// Ignore for now - in a real app we would probably like to log this
			// and maybe show error information in the UI depending on what is expected.
		}
	}

	/// <summary>
	/// Do a single read, handle errors, does not catch <see cref="OperationCanceledException"/>.
	/// </summary>
	/// <returns>An empty array in case of any errors (file-related errors are mainly expected here).</returns>
	private async Task<IReadOnlyList<Weapon>> ReadWeaponsAsync(CancellationToken ct)
	{
		await using var readStream = File.OpenRead(_pathToMonitor);
		var weapons = await JsonSerializer.DeserializeAsync<List<Weapon>>(readStream, _jsonOptions, ct);

		return weapons != null ? weapons : Array.Empty<Weapon>();
	}
}