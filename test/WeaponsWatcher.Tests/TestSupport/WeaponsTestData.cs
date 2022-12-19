namespace WeaponsWatcher.Tests.TestSupport;

internal static class WeaponsTestData
{
	public static string Json1 = @"
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

	public static string Json2 = @"
[
	{
		""Name"": ""Constitutional Arms Liberty"",
		""Tech"": ""Power"",
		""AttacksPerSecond"": 3.75
	},
	{
		""Name"": ""Tsunami Nekomata"",
		""Tech"": ""Tech"",
		""AttacksPerSecond"": 0.93
	}
]
";

	public static IEnumerable<Weapon> ExpectedWeapons1 = new List<Weapon>
	{
		new("Fenrir", TechType.Power, 6.9),
		new("Genjiroh", TechType.Smart, 4.8)
	};

	public static IEnumerable<Weapon> ExpectedWeapons2 = new List<Weapon>
	{
		new("Constitutional Arms Liberty", TechType.Power, 3.75),
		new("Tsunami Nekomata", TechType.Tech, 0.93)
	};
}