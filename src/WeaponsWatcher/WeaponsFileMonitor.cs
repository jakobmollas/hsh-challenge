using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeaponsWatcher;

// A different approach to using a PeriodicTimer (which is async) could be to use Dispatcher.BeginInvoke and run short diff checks on the target file.
// That would simplify the solution somewhat (no need to marshall calls to the UI thread, no need for separate threads) but it could also remove some separation of concerns.
// I am also not sure how to schedule an update to not run immediately, but in x milliseconds or similar.

// We could also design the class to not use IDisposable if that would be a better fit for the consumer
// by taking a taking a cancellation token in the constructor (not recommended),
// or by redesigning the class to have an explicit "run" method that takes a CancellationToken
// and returns a task that can be awaited by the consumer.
// That would give more control to the consumer and also allows the consumer to await the task to process any exceptions.
// It does however complicate the usage a bit and may be overly complex.
// As always, "it depends".

internal sealed class WeaponsFileMonitor : IAsyncDisposable
{
	private static readonly JsonSerializerOptions _jsonOptions = new() { Converters = { new JsonStringEnumConverter() } };

	private readonly CancellationTokenSource _cts = new();
	private readonly Task _monitorTask;
	private readonly IFileSystem _fileSystem;
	private readonly string _pathToMonitor;
	private readonly IPeriodicTimer _timer;
	private DateTime _lastWriteTime;

	public event EventHandler<IEnumerable<Weapon>>? WeaponsUpdated;

	/// <summary>
	/// Create a new <see cref="WeaponsFileMonitor"/> that continuously monitors <paramref name="pathToMonitor"/>
	/// for changes (using LastWriteTimeUtc) with an poll interval set to <paramref name="period"/>.
	/// When the file is modified, <see cref="WeaponsUpdated"/> will be fired with deserialized content.
	/// Any read errors are caught and will not result in a change event. 
	/// Dispose the instance to terminate the monitoring operation.
	/// </summary>
	/// <param name="pathToMonitor">Full path to file to monitor for changes (cannot be empty).</param>
	/// <param name="period">Poll rate (cannot be equal to or less than 0).</param>
	public WeaponsFileMonitor(string pathToMonitor, TimeSpan period)
		: this(pathToMonitor, new FileSystem(), new PeriodicTimer(period))
	{
	}

	/// <summary>
	/// Optional constructor, primarily used for testing.
	/// </summary>
	public WeaponsFileMonitor(string pathToMonitor, IFileSystem fileSystem, IPeriodicTimer timer)
	{
		if (string.IsNullOrWhiteSpace(pathToMonitor))
			throw new ArgumentException("Value cannot be null or whitespace.", nameof(pathToMonitor));

		_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
		_timer = timer ?? throw new ArgumentNullException(nameof(timer));

		_pathToMonitor = pathToMonitor;

		_monitorTask = Task.Run(async () => await RunAsync(_cts.Token));
	}

	/// <summary>
	/// Terminate monitoring and clean up.
	/// </summary>
	/// <remarks>
	/// Disposal should not throw exception unless there are some serious unexpected errors (bugs),
	/// meaning calling code do not need to catch exceptions, including <see cref="OperationCanceledException"/>.
	/// </remarks>
	public async ValueTask DisposeAsync()
	{
		try
		{
			_cts.Cancel();

			// If the task faulted for some unexpected reason, this will ensure any exceptions are re-thrown.
			// We do this to prevent any unexpected exceptions from being hidden.
			await _monitorTask;
		}
		finally
		{
			// Ensure we clean up no matter what
			_timer.Dispose();
			_cts.Dispose();
		}
	}

	private async Task RunAsync(CancellationToken ct)
	{
		try
		{
			while (!ct.IsCancellationRequested)
			{
				// Disposing _timer or cancelling token _should_ not result in an OperationCanceledException being thrown
				// based on documentation but it is not super clear. Expect an exception in any case.
				bool result = await _timer.WaitForNextTickAsync(ct);
				if (!result)
					return;

				await CheckForUpdatesAsync(ct);
			}
		}
		catch (OperationCanceledException)
		{
			// Ok
		}
	}

	private async Task CheckForUpdatesAsync(CancellationToken ct)
	{
		try
		{
			// No listeners or nothing to read? No point in working hard in that case.
			// By checking for subscribers we also handle an edge case where a (single) consumer creates a monitor
			// that starts and processes a file BEFORE the consumer has time to subscribe to the change event,
			// which would lead to a lost initial update.
			if (WeaponsUpdated == null || !_fileSystem.File.Exists(_pathToMonitor))
				return;

			var writeTime = _fileSystem.File.GetLastWriteTimeUtc(_pathToMonitor);
			if (writeTime == _lastWriteTime)
				return;

			_lastWriteTime = writeTime;

			var weapons = await ReadWeaponsListAsync(_fileSystem, _pathToMonitor, ct);

			WeaponsUpdated?.Invoke(this, weapons);
		}
		catch (Exception)
		{
			// Ignore for now - in a real app we may want to log this
			// and maybe show error information in the UI depending on what is expected.
		}
	}

	private static async Task<IReadOnlyList<Weapon>> ReadWeaponsListAsync(IFileSystem fileSystem, string path, CancellationToken ct)
	{
		await using var readStream = fileSystem.File.OpenRead(path);
		var weapons = await JsonSerializer.DeserializeAsync<List<Weapon>>(readStream, _jsonOptions, ct);

		return weapons != null ? weapons : Array.Empty<Weapon>();
	}
}