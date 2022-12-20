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
	public async Task Missing_Initial_File_Content_Is_Ignored()
	{
		// Arrange
		await using var monitor = new WeaponsFileMonitor(@"c:\non-existing.json", _fileSystem, _timer);

		int eventsFired = 0;
		monitor.WeaponsUpdated += (_, _) => eventsFired++;

		// Act
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(0, eventsFired);
	}

	[Fact]
	public async Task Invalid_Initial_File_Results_In_Empty_Array()
	{
		// Arrange
		_fileSystem.AddFile(@"c:\weapons.json", new MockFileData("{") { LastWriteTime = DateTime.UtcNow });

		await using var monitor = new WeaponsFileMonitor(@"c:\weapons.json", _fileSystem, _timer);

		int eventsFired = 0;
		var updatedWeapons = new List<Weapon>();
		monitor.WeaponsUpdated += (_, weapons) => { updatedWeapons = weapons.ToList(); eventsFired++; };

		// Act - wait for two iterations, only one event should be created
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(1, eventsFired);
		Assert.Empty(updatedWeapons);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Valid_Initial_File_Is_Processed_Successfully(bool subscribeAfterInitialPoll)
	{
		// Arrange
		_fileSystem.AddFile(@"c:\weapons.json", new MockFileData(WeaponsTestData.Json1) { LastWriteTime = DateTime.UtcNow });

		await using var monitor = new WeaponsFileMonitor(@"c:\weapons.json", _fileSystem, _timer);

		// It should not matter if we subscribe _after_ the polling starts (as long as no existing subscribers exists).
		if (subscribeAfterInitialPoll)
			await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		int eventsFired = 0;
		var updatedWeapons = new List<Weapon>();
		monitor.WeaponsUpdated += (_, weapons) => { updatedWeapons = weapons.ToList(); eventsFired++; };

		// Act - wait for two iterations, only one event should be created
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(1, eventsFired);
		Assert.Equal(WeaponsTestData.ExpectedWeapons1, updatedWeapons);
	}

	[Fact]
	public async Task Valid_Updated_File_Is_Processed_As_Expected()
	{
		// Arrange
		var file = new MockFileData(WeaponsTestData.Json1) { LastWriteTime = DateTime.UtcNow };
		_fileSystem.AddFile(@"c:\weapons.json", file);

		await using var monitor = new WeaponsFileMonitor(@"c:\weapons.json", _fileSystem, _timer);

		int eventsFired = 0;
		var updatedWeapons = new List<Weapon>();
		monitor.WeaponsUpdated += (_, weapons) => { updatedWeapons = weapons.ToList(); eventsFired++; };

		// Act - initial read
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);
		Assert.Equal(1, eventsFired);

		// Update file
		file.TextContents = WeaponsTestData.Json2;
		file.LastWriteTime = DateTime.UtcNow.AddSeconds(1);

		// Wait for two iterations, only one event should be created here
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(2, eventsFired);
		Assert.Equal(WeaponsTestData.ExpectedWeapons2, updatedWeapons);
	}

	[Fact]
	public async Task No_Updated_File_Write_Time_Does_Not_Fire_Event()
	{
		// Arrange
		var file = new MockFileData(WeaponsTestData.Json1) { LastWriteTime = DateTime.UtcNow };
		_fileSystem.AddFile(@"c:\weapons.json", file);

		await using var monitor = new WeaponsFileMonitor(@"c:\weapons.json", _fileSystem, _timer);

		int eventsFired = 0;
		var updatedWeapons = new List<Weapon>();
		monitor.WeaponsUpdated += (_, weapons) => { updatedWeapons = weapons.ToList(); eventsFired++; };

		// Act - initial read
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);
		Assert.Equal(1, eventsFired);

		// Update file but do NOT update write time
		file.TextContents = WeaponsTestData.Json2;

		// No event should be created here
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(1, eventsFired);
		Assert.Equal(WeaponsTestData.ExpectedWeapons1, updatedWeapons);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Deleted_File_Or_Invalid_File_Update_Results_In_Empty_List(bool deleteFile)
	{
		// Arrange
		var file = new MockFileData(WeaponsTestData.Json1) { LastWriteTime = DateTime.UtcNow };
		_fileSystem.AddFile(@"c:\weapons.json", file);

		await using var monitor = new WeaponsFileMonitor(@"c:\weapons.json", _fileSystem, _timer);

		int eventsFired = 0;
		var updatedWeapons = new List<Weapon>();
		monitor.WeaponsUpdated += (_, weapons) => { updatedWeapons = weapons.ToList(); eventsFired++; };
		
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);
		Assert.Equal(1, eventsFired);

		// Act - delete/update file (split into two tests if this becomes too complicated)
		if (deleteFile)
		{
			_fileSystem.RemoveFile(@"c:\weapons.json");
		}
		else
		{
			file.TextContents = "{";
			file.LastWriteTime = DateTime.UtcNow.AddSeconds(1);
		}

		// Wait for two iterations, only one additional event should be created here
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);
		await _timer.ReleaseNextTickAndWaitForFullTickProcessingAsync(_cts.Token);

		// Assert
		Assert.Equal(2, eventsFired);
		Assert.Empty(updatedWeapons);
	}

	public void Dispose()
	{
		_timer.Dispose();
		_cts.Dispose();
	}
}