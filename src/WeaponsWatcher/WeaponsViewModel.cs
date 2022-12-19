using System.Collections.ObjectModel;

namespace WeaponsWatcher;

public sealed class WeaponsViewModel
{
	public ObservableCollection<Weapon> Weapons { get; } = new();

	public void UpdateWeapons(IEnumerable<Weapon> weapons)
	{
		// This can be improved - we do a brute-force update currently.
		// Instead we could do a diff update and avoid updating if no data actually changed.
		// Also - there is probably a way to temporary disable binding/updates, process updates,
		// enable again (to avoid multiple update events in the UI) and trigger event if something actually changed.
		// I cannot figure out how to do that in WPF though.
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