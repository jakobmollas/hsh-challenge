namespace WeaponsWatcher;

internal sealed class PeriodicTimer : IPeriodicTimer
{
	private readonly System.Threading.PeriodicTimer _periodicTimer;

	// Todo: Add basic tests

	/// <summary>
	/// Create a new timer based on <see cref="System.Threading.PeriodicTimer"/> with a poll interval set to <paramref name="period"/>.
	/// </summary>
	/// <param name="period">Polling period, must be greater than 0.</param>
	public PeriodicTimer(TimeSpan period)
	{
		if (period <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(period), $"{nameof(period)} must be greater than 0.");

		_periodicTimer = new System.Threading.PeriodicTimer(period);
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