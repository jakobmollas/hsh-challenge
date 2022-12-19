namespace WeaponsWatcher.Tests.TestSupport;

internal sealed class MockTimer : IPeriodicTimer
{
	private readonly TestLogger _logger;
	private readonly SemaphoreSlim _nextTickSemaphore = new(0);
	private readonly SemaphoreSlim _tickConsumedSemaphore = new(0);
	private readonly SemaphoreSlim _tickAwaitedSemaphore = new(0);
	private int _consumerAwaitCount;

	public MockTimer(TestLogger logger)
	{
		_logger = logger;
	}

	public async ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken)
	{
		_logger.Log($"Consumer awaited {nameof(WaitForNextTickAsync)} ({++_consumerAwaitCount} times).");
		_tickAwaitedSemaphore.Release();

		await _nextTickSemaphore.WaitAsync(cancellationToken);
		_logger.Log("Consumer is processing tick...");

		// Handle initial await where test code have not yet awaited the initial consumer tick await
		if (_tickAwaitedSemaphore.CurrentCount > 0)
		{
			_logger.Log($"Consuming initial {nameof(_tickAwaitedSemaphore)}.");
			await _tickAwaitedSemaphore.WaitAsync(cancellationToken);
		}

		_tickConsumedSemaphore.Release();

		return true;
	}

	internal async ValueTask ReleaseNextTickAndWaitForFullTickProcessingAsync(CancellationToken cancellationToken)
	{
		await ReleaseNextTickAndWaitConsumerToProcessTickAsync(cancellationToken);
		await WaitForConsumerToWaitForNextTickAsync(cancellationToken);
	}

	internal async ValueTask ReleaseNextTickAndWaitConsumerToProcessTickAsync(CancellationToken cancellationToken)
	{
		var consumptionTask = _tickConsumedSemaphore.WaitAsync(cancellationToken);

		_logger.Log($"Releasing {nameof(WaitForNextTickAsync)}.");
		_nextTickSemaphore.Release();

		_logger.Log($"Waiting for consumer to await {nameof(WaitForNextTickAsync)}.");
		await consumptionTask;

		_logger.Log($"Finished waiting for {nameof(WaitForNextTickAsync)} consumption.");
	}

	internal async ValueTask WaitForConsumerToWaitForNextTickAsync(CancellationToken cancellationToken)
	{
		_logger.Log($"Waiting for consumer to await next {nameof(WaitForNextTickAsync)}.");
		await _tickAwaitedSemaphore.WaitAsync(cancellationToken);
		_logger.Log($"Finished waiting for next {nameof(WaitForNextTickAsync)} await.");
	}

	public void Dispose()
	{
		_nextTickSemaphore.Dispose();
		_tickConsumedSemaphore.Dispose();
		_tickAwaitedSemaphore.Dispose();
	}
}