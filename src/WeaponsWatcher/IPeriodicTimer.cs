namespace WeaponsWatcher;

/// <summary>
/// Simple wrapper around <see cref="System.Threading.PeriodicTimer"/>,
/// used to simplify testing.
/// </summary>
internal interface IPeriodicTimer : IDisposable
{
	ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken);
}