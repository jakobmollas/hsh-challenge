using System.IO.Abstractions.TestingHelpers;
using Moq;
using WeaponsWatcher.Tests.TestSupport;
using Xunit;
using Xunit.Abstractions;

namespace WeaponsWatcher.Tests;

public sealed class WeaponsViewModelTests : IDisposable
{
	private readonly CancellationTokenSource _cts;
	private readonly MockFileSystem _fileSystem;
	private readonly MockTimer _timer;

	public WeaponsViewModelTests(ITestOutputHelper outputHelper)
	{
		// Ensure we terminate any uncompleted operations after a while by using a timeout.
		_cts = new(TimeSpan.FromSeconds(3));
		_fileSystem = new(new Dictionary<string, MockFileData>());
		_timer = new(new TestLogger(outputHelper));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Monitor_With_Bad_Params_Throws(string badPath)
	{
		var fileSystem = new MockFileSystem();
		var timer = Mock.Of<IPeriodicTimer>();
		var interval = TimeSpan.FromSeconds(1);

		// Primary ctor
		Assert.Throws<ArgumentException>("pathToMonitor", () => new WeaponsFileMonitor(badPath, interval));
		Assert.Throws<ArgumentOutOfRangeException>("period", () => new WeaponsFileMonitor(badPath, TimeSpan.Zero));

		// Secondary ctor

		Assert.Throws<ArgumentException>("pathToMonitor", () => new WeaponsFileMonitor(badPath, fileSystem, timer));
		Assert.Throws<ArgumentNullException>("fileSystem", () => new WeaponsFileMonitor("test.json", null!, timer));
		Assert.Throws<ArgumentNullException>("timer", () => new WeaponsFileMonitor("test.json", fileSystem, null!));
	}

	[Fact]
	public async Task Non_Zero_Interval_In_Ctor_Is_Ok()
	{
		await using var monitor = new WeaponsFileMonitor("test.json", TimeSpan.FromSeconds(1));
		Assert.NotNull(monitor);
	}

	[Fact]
	public async Task Missing_File_Content_Is_Ignored()
	{
		// Arrange
		await using var monitor = new WeaponsFileMonitor(@"c:\non-existing.json", _fileSystem, _timer);

		bool eventFired = false;
		monitor.WeaponsUpdated += (_, _) => eventFired = true;

		// Act
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.False(eventFired);
	}

	[Fact]
	public async Task Invalid_File_Content_Is_Ignored()
	{
		// Arrange
		_fileSystem.AddFile(@"c:\weapons.json", new MockFileData("{") { LastWriteTime = DateTime.UtcNow });

		await using var monitor = new WeaponsFileMonitor(@"c:\weapons.json", _fileSystem, _timer);

		bool eventFired = false;
		monitor.WeaponsUpdated += (_, _) => eventFired = true;

		// Act
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.False(eventFired);
	}

	[Fact]
	public async Task Late_Event_Subscription_Is_Processed_As_Expected()
	{
		// Arrange
		_fileSystem.AddFile(@"c:\weapons.json", new MockFileData(WeaponsTestData.Json1) { LastWriteTime = DateTime.UtcNow });

		await using var monitor = new WeaponsFileMonitor(@"c:\weapons.json", _fileSystem, _timer);

		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Act - add subscriber AFTER first pass
		var updatedWeapons = new List<Weapon>();
		monitor.WeaponsUpdated += (_, weapons) => updatedWeapons = weapons.ToList();

		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(WeaponsTestData.ExpectedWeapons1, updatedWeapons);
	}

	[Fact]
	public async Task Existing_Data_Is_Processed_As_Expected()
	{
		// Arrange
		_fileSystem.AddFile(@"c:\weapons.json", new MockFileData(WeaponsTestData.Json1) { LastWriteTime = DateTime.UtcNow });

		await using var monitor = new WeaponsFileMonitor(@"c:\weapons.json", _fileSystem, _timer);

		var updatedWeapons = new List<Weapon>();
		monitor.WeaponsUpdated += (_, weapons) => updatedWeapons = weapons.ToList();

		// Act
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(WeaponsTestData.ExpectedWeapons1, updatedWeapons);
	}

	[Fact]
	public async Task Updated_File_Is_Processed_As_Expected()
	{
		// Arrange
		var file = new MockFileData(WeaponsTestData.Json1) { LastWriteTime = DateTime.UtcNow };
		_fileSystem.AddFile(@"c:\weapons.json", file);

		await using var monitor = new WeaponsFileMonitor(@"c:\weapons.json", _fileSystem, _timer);

		int eventCount = 0;
		var updatedWeapons = new List<Weapon>();
		monitor.WeaponsUpdated += (_, weapons) =>
		{
			updatedWeapons = weapons.ToList();
			eventCount++;
		};

		// Act - initial read
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);
		
		// Update file
		file.TextContents = WeaponsTestData.Json2;
		file.LastWriteTime = DateTime.UtcNow.AddSeconds(1);

		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(2, eventCount);
		Assert.Equal(WeaponsTestData.ExpectedWeapons2, updatedWeapons);
	}

	[Fact]
	public async Task No_Updated_File_Write_Time_Does_Not_Fire_Event()
	{
		// Arrange
		var file = new MockFileData(WeaponsTestData.Json1) { LastWriteTime = DateTime.UtcNow };
		_fileSystem.AddFile(@"c:\weapons.json", file);

		await using var monitor = new WeaponsFileMonitor(@"c:\weapons.json", _fileSystem, _timer);

		int eventCount = 0;
		var updatedWeapons = new List<Weapon>();
		monitor.WeaponsUpdated += (_, weapons) =>
		{
			updatedWeapons = weapons.ToList();
			eventCount++;
		};

		// Act - initial read
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Update file but do NOT update write time
		file.TextContents = WeaponsTestData.Json2;
		
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(1, eventCount);
		Assert.Equal(WeaponsTestData.ExpectedWeapons1, updatedWeapons);
	}

	public void Dispose()
	{
		_timer.Dispose();
		_cts.Dispose();
	}
}