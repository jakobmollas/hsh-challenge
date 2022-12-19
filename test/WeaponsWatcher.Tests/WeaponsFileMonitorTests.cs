using System.IO.Abstractions.TestingHelpers;
using Moq;
using WeaponsWatcher.Tests.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace WeaponsWatcher.Tests;

public sealed class WeaponsViewModelTests : IDisposable
{
	// Ensure we terminate any uncompleted operations after a while
	
	private readonly CancellationTokenSource _cts;
	private readonly MockFileSystem _fileSystem;
	private readonly MockTimer _timer;

	public WeaponsViewModelTests(ITestOutputHelper outputHelper)
	{
		_cts = new(TimeSpan.FromSeconds(3));
		_fileSystem = new(new Dictionary<string, MockFileData>());
		_timer = new(new TestLogger(outputHelper));
	}

	[Fact]
	public void Monitor_With_Bad_Params_Throws()
	{
		var fileSystem = new MockFileSystem();
		var timer = Mock.Of<IPeriodicTimer>();

		Assert.Throws<ArgumentNullException>("fileSystem", () => new WeaponsFileMonitor(null!, "test.txt", timer));

		Assert.Throws<ArgumentException>("pathToMonitor", () => new WeaponsFileMonitor(fileSystem, null!, timer));
		Assert.Throws<ArgumentException>("pathToMonitor", () => new WeaponsFileMonitor(fileSystem, "", timer));
		Assert.Throws<ArgumentException>("pathToMonitor", () => new WeaponsFileMonitor(fileSystem, "   ", timer));

		Assert.Throws<ArgumentNullException>("timer", () => new WeaponsFileMonitor(fileSystem, "test", null!));
	}

	[Fact]
	public async Task Valid_Data_Is_Read_And_Passed_Via_Event()
	{
		// Arrange
		_fileSystem.AddFile(@"c:\weapons.json", new MockFileData(WeaponsTestData.Json1) { LastWriteTime = DateTime.UtcNow });

		await using var monitor = new WeaponsFileMonitor(_fileSystem, @"c:\weapons.json", _timer);

		var updatedWeapons = new List<Weapon>();
		monitor.WeaponsUpdated += (_, weapons) => updatedWeapons = weapons.ToList();

		// Act
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(WeaponsTestData.ExpectedWeapons1, updatedWeapons);
	}

	[Fact]
	public async Task Invalid_File_Content_Is_Ignored()
	{
		// Arrange
		_fileSystem.AddFile(@"c:\weapons.json", new MockFileData("{") { LastWriteTime = DateTime.UtcNow });

		await using var monitor = new WeaponsFileMonitor(_fileSystem, @"c:\weapons.json", _timer);

		bool eventFired = false;
		monitor.WeaponsUpdated += (_, _) => eventFired = true;

		// Act
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.False(eventFired);
	}

	[Fact]
	public async Task Updated_File_Is_Processed()
	{
		// Arrange
		var file = new MockFileData(WeaponsTestData.Json1) { LastWriteTime = DateTime.UtcNow };
		_fileSystem.AddFile(@"c:\weapons.json", file);

		await using var monitor = new WeaponsFileMonitor(_fileSystem, @"c:\weapons.json", _timer);

		int eventCount = 0;
		var updatedWeapons = new List<Weapon>();
		monitor.WeaponsUpdated += (_, weapons) =>
		{
			updatedWeapons = weapons.ToList();
			eventCount++;
		};

		// Act - initial read
		await _timer.ReleaseNextTickAndWaitConsumerToProcessTickAsync(_cts.Token);
		await _timer.WaitForConsumerToWaitForNextTickAsync(_cts.Token);

		// Update file
		file.TextContents = WeaponsTestData.Json2;
		file.LastWriteTime = DateTime.UtcNow.AddSeconds(1);

		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(2, eventCount);
		Assert.Equal(WeaponsTestData.ExpectedWeapons2, updatedWeapons);
	}

	public void Dispose()
	{
		_timer.Dispose();
		_cts.Dispose();
	}
}