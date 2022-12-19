using System.Diagnostics.CodeAnalysis;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace WeaponsWatcher.Tests.TestSupport;

[ExcludeFromCodeCoverage]
internal sealed class TestLogger
{
	private readonly TestOutputHelper _outputHelper;

	public TestLogger(ITestOutputHelper outputHelper) => _outputHelper = (TestOutputHelper)outputHelper;

	public void Log(string message) => _outputHelper.WriteLine($"{DateTime.Now:hh:mm:ss.fff}: {message}");
}