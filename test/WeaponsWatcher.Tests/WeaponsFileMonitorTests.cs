using Xunit;

namespace WeaponsWatcher.Tests;

public class WeaponsViewModelTests
{
	[Fact]
	public void Monitor_With_Bad_Params_Throws()
	{
		Assert.Throws<ArgumentException>("pathToMonitor", () => new WeaponsFileMonitor(null!, TimeSpan.Zero));
		Assert.Throws<ArgumentException>("pathToMonitor", () => new WeaponsFileMonitor("", TimeSpan.Zero));
		Assert.Throws<ArgumentException>("pathToMonitor", () => new WeaponsFileMonitor("   ", TimeSpan.Zero));

		Assert.Throws<ArgumentOutOfRangeException>("interval", () => new WeaponsFileMonitor("test", TimeSpan.Zero));
		Assert.Throws<ArgumentOutOfRangeException>("interval", () => new WeaponsFileMonitor("test", TimeSpan.FromSeconds(-1)));
	}
}