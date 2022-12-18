using System.IO.Abstractions.TestingHelpers;
using Xunit;

namespace WeaponsWatcher.Tests;

public class WeaponsViewModelTests
{
	[Fact]
	public void Monitor_With_Bad_Params_Throws()
	{
		var fileSystem = new MockFileSystem();

		Assert.Throws<ArgumentNullException>("fileSystem", () => new WeaponsFileMonitor(null!, "test.txt", TimeSpan.Zero));

		Assert.Throws<ArgumentException>("pathToMonitor", () => new WeaponsFileMonitor(fileSystem, null!, TimeSpan.Zero));
		Assert.Throws<ArgumentException>("pathToMonitor", () => new WeaponsFileMonitor(fileSystem, "", TimeSpan.Zero));
		Assert.Throws<ArgumentException>("pathToMonitor", () => new WeaponsFileMonitor(fileSystem, "   ", TimeSpan.Zero));

		Assert.Throws<ArgumentOutOfRangeException>("interval", () => new WeaponsFileMonitor(fileSystem, "test", TimeSpan.Zero));
		Assert.Throws<ArgumentOutOfRangeException>("interval", () => new WeaponsFileMonitor(fileSystem, "test", TimeSpan.FromSeconds(-1)));
	}

	[Fact]
	public async Task Monitor_With_Bad_File_Contents_Does_Not_Fire_Event()
	{
		// Arrange
		var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());
		fileSystem.AddFile(@"c:\weapons.json", new MockFileData(TestData.WeaponsJson1));

		var updatedWeapons = new List<Weapon>();

		var monitor = new WeaponsFileMonitor(fileSystem, @"c:\weapons.json", TimeSpan.FromMilliseconds(1));
		monitor.WeaponsUpdated += (_, weapons) => updatedWeapons = weapons.ToList();

		// Todo: Fix this - avoid relying on timings
		// Act
		await Task.Delay(100);

		// Assert
		Assert.Equal(TestData.WeaponsList1, updatedWeapons);
	}
}

public static class TestData
{
	public static string WeaponsJson1 = @"
[
	{
		""Name"": ""Fenrir"",
		""Tech"": ""Power"",
		""AttacksPerSecond"": 6.9
	},
	{
		""Name"": ""Genjiroh"",
		""Tech"": ""Smart"",
		""AttacksPerSecond"": 4.8
	}
]
";

	public static IEnumerable<Weapon> WeaponsList1 = new List<Weapon>
	{
		new("Fenrir", TechType.Power, 6.9),
		new("Genjiroh", TechType.Smart, 4.8)
	};
}