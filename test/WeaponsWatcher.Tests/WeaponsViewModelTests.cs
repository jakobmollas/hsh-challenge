using KellermanSoftware.CompareNetObjects;
using Xunit;

namespace WeaponsWatcher.Tests;

public class WeaponsViewModelTests
{
	[Fact]
	public void Update_Clears_And_Updates_Collection()
	{
		// Arrange
		var viewModel = new WeaponsViewModel();
		Assert.Empty(viewModel.Weapons);

		IEnumerable<Weapon> newWeapons = CreateWeapons();

		// Act
		viewModel.UpdateWeapons(newWeapons);

		// Assert
		viewModel.Weapons.ShouldCompare(newWeapons, compareConfig: new ComparisonConfig { IgnoreObjectTypes = true });
	}

	[Fact]
	public void Clears_Empties_Collection()
	{
		// Arrange
		var viewModel = new WeaponsViewModel();

		var newWeapons = CreateWeapons();
		viewModel.UpdateWeapons(newWeapons);

		// Act
		viewModel.ClearWeapons();

		// Assert
		Assert.Empty(viewModel.Weapons);
	}

	private static List<Weapon> CreateWeapons()
	{
		return new List<Weapon>
		{
			new("Boomerang", TechType.Power, 0.001),
			new("ÚSB-drive", TechType.Tech, 4.4),
			new("Rubik Cube", TechType.Smart, 12.55)
		};
	}
}