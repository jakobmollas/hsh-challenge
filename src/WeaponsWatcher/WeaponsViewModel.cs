using System.Collections.ObjectModel;

namespace WeaponsWatcher;

public sealed class WeaponsViewModel
{
	public ObservableCollection<Weapon> Weapons { get; } = new();

	public void UpdateWeapons(IEnumerable<Weapon> weapons)
	{
		// This can definitely be improved - we do a brute-force update.
		// Instead we could to a diff update and also avoid updating if not needed.
		// Also - there is probably a way to temporary disable binding/updates,
		// do updates, enable again (to avoid multiple update events in the UI) but I cannot figure that out.
		ClearWeapons();

		foreach (var weapon in weapons)
			Weapons.Add(weapon);
	}

	public void ClearWeapons()
	{
		Weapons.Clear();
	}
}

public sealed record Weapon(string Name, TechType Tech, double AttacksPerSecond);

public enum TechType
{
	Tech,
	Smart,
	Power
}