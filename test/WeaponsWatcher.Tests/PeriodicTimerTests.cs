namespace WeaponsWatcher.Tests;

public class PeriodicTimerTests
{
	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Bad_Interval_Throws(int secondsOffset)
	{
		Assert.Throws<ArgumentOutOfRangeException>("period", () => new PeriodicTimer(TimeSpan.FromSeconds(secondsOffset)));
	}

	[Fact]
	public void Dispose_Does_Not_Throw()
	{
		var timer = new PeriodicTimer(TimeSpan.FromHours(1));
		Assert.NotNull(timer);

		timer.Dispose();
	}

	[Fact]
	public void Positive_Period_Is_Ok()
	{
		var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
		Assert.NotNull(timer);
	}

	[Fact]
	public async Task WaitForNextTickAsync_Throws_On_Cancel()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

		// Act/assert
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await timer.WaitForNextTickAsync(cts.Token));
	}

	[Fact]
	public async Task WaitForNextTickAsync_Works_As_Expected()
	{
		// Just a sanity check, it is always hard to test time-dependent code.

		// Arrange
		using var cts = new CancellationTokenSource();
		var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));

		// Act
		bool result = await timer.WaitForNextTickAsync(cts.Token);

		// Assert
		Assert.True(result);
	}
}