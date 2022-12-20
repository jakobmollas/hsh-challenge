using SystemPeriodicTimer = System.Threading.PeriodicTimer;

namespace WeaponsWatcher;

internal sealed class PeriodicTimer : IPeriodicTimer
{
	private readonly SystemPeriodicTimer _periodicTimer;

	/// <summary>
	/// Create a new timer based on <see cref="SystemPeriodicTimer"/> with a poll interval set to <paramref name="period"/>.
	/// </summary>
	/// <param name="period">Polling period, must be greater than 0.</param>
	public PeriodicTimer(TimeSpan period)
	{
		if (period <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(period), $"{nameof(period)} must be greater than 0.");

		_periodicTimer = new SystemPeriodicTimer(period);
	}

	public ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken)
	{
		return _periodicTimer.WaitForNextTickAsync(cancellationToken);
	}

	public void Dispose()
	{
		_periodicTimer.Dispose();
	}
}