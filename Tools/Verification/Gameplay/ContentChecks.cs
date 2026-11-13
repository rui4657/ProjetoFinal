using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

static partial class Program
{
    static void ContentChecks()
    {
        Test("New runs start with the pistol and a single occupied slot", () => {
            var m = new Match(); m.Gun.InitNewGame();
            Assert(m.Gun.ActiveWeapon == WeaponId.Pistol && m.Gun.currentAmmo == 12 && m.Gun.reserveAmmo == 60
                && m.Gun.Loadout.Get(1) == null && !WeaponCatalog.Get(m.Gun.ActiveWeapon).Automatic, "New run kept the legacy rifle");
        });
        Test("Pistol requires fresh presses while SMG accepts held fire", () => {
            var m = new Match(); m.MakeWorld(); m.Gun.InitNewGame(); Cursor.lockState = CursorLockMode.Locked;
            Input.MouseHeld.Add(0); Call(m.Gun, "Update"); Assert(m.Session.ShotsFired == 0, "Semiautomatic accepted held input");
            Input.MouseDown.Add(0); Call(m.Gun, "Update"); Input.MouseDown.Clear(); Time.time = .3f; Call(m.Gun, "Update");
            Assert(m.Session.ShotsFired == 1, "Pistol repeated without a fresh press");
            m.Session.ApplySavedProgress(new GameState { points = 2000 }); Assert(m.Gun.PurchaseWeapon(WeaponId.SMG), "SMG purchase failed");
            Time.time = 1; Call(m.Gun, "Update"); Assert(m.Session.ShotsFired == 2 && m.Gun.currentAmmo == 31, "Automatic fire did not resume after draw");
        });
        Test("Two-weapon switching conserves both magazines and reserves", () => {
            var m = new Match(); m.Gun.InitNewGame(); m.Session.ApplySavedProgress(new GameState { points = 5000 });
            m.Gun.currentAmmo = 5; m.Gun.reserveAmmo = 19; m.Gun.PurchaseWeapon(WeaponId.SMG);
            m.Gun.currentAmmo = 23; m.Gun.reserveAmmo = 77; Time.time = 1;
            Assert(m.Gun.TrySwitchWeapon(0) && m.Gun.currentAmmo == 5 && m.Gun.reserveAmmo == 19, "Switch refilled the pistol");
            Assert(!m.Gun.TrySwitchWeapon(1), "Draw delay was bypassed"); Time.time = 2;
            Assert(m.Gun.TrySwitchWeapon(1) && m.Gun.currentAmmo == 23 && m.Gun.reserveAmmo == 77, "Switch refilled the SMG");
            Time.timeScale = 0; Assert(!m.Gun.TrySwitchWeapon(0), "Paused switch changed inventory");
        });
        Test("A third weapon replaces the active slot and keeps the other weapon", () => {
            var m = new Match(); m.Gun.InitNewGame(); m.Session.ApplySavedProgress(new GameState { points = 10000 });
            m.Gun.PurchaseWeapon(WeaponId.SMG); Time.time = 1; m.Gun.TrySwitchWeapon(0); Time.time = 2;
            Assert(m.Gun.PurchaseWeapon(WeaponId.Shotgun), "Third purchase failed");
            Assert(m.Gun.Loadout.Get(0).Id == WeaponId.Shotgun && m.Gun.Loadout.Get(1).Id == WeaponId.SMG
                && m.Gun.Loadout.IndexOf(WeaponId.Pistol) < 0 && m.Session.Points == 7300, "Third purchase replaced both slots or charged incorrectly");
        });
        Test("Wall ammunition refills an owned inactive weapon without duplicating rewards", () => {
            var m = new Match(); m.Gun.InitNewGame(); m.Session.ApplySavedProgress(new GameState { points = 10000 });
            m.Gun.currentAmmo = 7; m.Gun.PurchaseWeapon(WeaponId.SMG); m.Gun.currentAmmo = 1; m.Gun.reserveAmmo = 0;
            Time.time = 1; m.Gun.TrySwitchWeapon(0); int points = m.Session.Points;
            Assert(m.Gun.PurchaseWeapon(WeaponId.SMG) && m.Session.Points == points - 450, "Owned weapon charged purchase price");
            Assert(m.Gun.ActiveWeapon == WeaponId.Pistol && m.Gun.currentAmmo == 7 && m.Gun.Loadout.Get(1).IsFull, "Inactive refill changed active ammunition");
            points = m.Session.Points; Assert(!m.Gun.PurchaseWeapon(WeaponId.SMG) && m.Session.Points == points, "Full wall weapon charged again");
        });
        Test("Loadout recovery clamps ammunition and rejects unknown duplicate or oversized slots", () => {
            foreach (string bad in new[] { "", "v2;0;0,1,2;-", "v1;0;5,1,2;-", "v1;0;0,1,2;0,3,4", "v1;1;0,1,2;-", new string('x', 200) })
                Assert(!WeaponLoadout.TryDecode(bad, out _), "Malformed inventory accepted: " + bad);
            Assert(WeaponLoadout.TryDecode("v1;1;0,-5,999;2,999,-2", out var slots), "Bounded recovery failed");
            Assert(slots.Get(0).Magazine == 0 && slots.Get(0).Reserve == 120 && slots.Current.Magazine == 8 && slots.Current.Reserve == 0, "Recovered capacities invalid");
            Assert(WeaponLoadout.TryDecode(slots.Encode(), out var again) && again.Active == 1, "Inventory roundtrip lost selected slot");
        });
        Test("Shotgun pellets respect walls and count one shot and one elimination", () => {
            var m = new Match(); m.MakeWorld(); m.Gun.InitNewGame(); m.Session.ApplySavedProgress(new GameState { points = 5000 });
            m.Gun.PurchaseWeapon(WeaponId.Shotgun); var zombie = Enemy(m, ZombieArchetype.Walker);
            var target = zombie.GetComponent<CapsuleCollider>(); var wall = new GameObject("Cover").AddComponent<BoxCollider>();
            Physics.NextHits = new[] { new RaycastHit { collider = target, distance = 2, point = target.bounds.center }, new RaycastHit { collider = wall, distance = 1 } };
            int points = m.Session.Points; Call(m.Gun, "ShootIfPossible");
            Assert(!zombie.IsDead && zombie.CurrentHealth == 100 && m.Session.Hits == 0, "Shotgun penetrated a wall");
            Physics.NextHits = new[] { new RaycastHit { collider = target, distance = 2, point = target.bounds.center } };
            Call(m.Gun, "ShootIfPossible");
            Assert(zombie.IsDead && m.Session.ShotsFired == 2 && m.Session.Hits == 1 && m.Session.Kills == 1
                && m.Session.Points == points + 60 && m.Gun.currentAmmo == 6, "Pellets inflated accuracy or kill rewards");
            Assert(WeaponCatalog.DamageAt(WeaponId.Shotgun, 32) < WeaponCatalog.DamageAt(WeaponId.Shotgun, 8), "Shotgun has no range falloff");
        });
        Test("Shotgun loads individual shells and can interrupt after one shell", () => {
            var m = new Match(); m.MakeWorld(); m.Gun.InitNewGame(); m.Session.ApplySavedProgress(new GameState { points = 5000 });
            m.Gun.PurchaseWeapon(WeaponId.Shotgun); Time.time = 1; m.Gun.currentAmmo = 0; m.Gun.reserveAmmo = 3;
            Call(m.Gun, "BeginReload"); var reload = m.Gun.Coroutines.Last(); reload.Tick();
            Assert(m.Gun.currentAmmo == 1 && m.Gun.reserveAmmo == 2 && m.Gun.IsReloading, "Shell reload transferred a full magazine");
            Cursor.lockState = CursorLockMode.Locked; Input.MouseDown.Add(0); Call(m.Gun, "Update");
            Assert(!m.Gun.IsReloading && m.Gun.currentAmmo == 0 && m.Gun.reserveAmmo == 2 && m.Session.ShotsFired == 1, "Loaded shell could not interrupt reload");
            reload.Tick(); Assert(m.Gun.currentAmmo == 0 && m.Gun.reserveAmmo == 2, "Cancelled shell reload completed later");
        });
        Test("Max ammo fills both slots but never grants an unowned weapon", () => {
            var m = new Match(); m.Gun.InitNewGame(); m.Session.ApplySavedProgress(new GameState { points = 5000 });
            m.Gun.currentAmmo = 0; m.Gun.reserveAmmo = 0; m.Gun.PurchaseWeapon(WeaponId.SMG); m.Gun.currentAmmo = 1; m.Gun.reserveAmmo = 2;
            m.Session.ActivatePowerUp(SurvivalPowerUp.MaxAmmo);
            Assert(m.Gun.currentAmmo == 32 && m.Gun.reserveAmmo == 256 && m.Gun.Loadout.Get(0).IsFull
                && m.Gun.Loadout.IndexOf(WeaponId.Rifle) < 0, "Max ammo lost the inactive slot or granted a weapon");
        });
        Test("Changing weapon models preserves the gun component and restores the authored AK muzzle", () => {
            var m = new Match(); m.MakeWorld(); m.Gun.InitNewGame();
            var holder = new GameObject("WeaponHolder").transform; holder.SetParent(m.Gun.playerCam.transform, false);
            holder.localPosition = new Vector3(.4f, -.3f, .8f);
            var rifle = new GameObject("Authored AK").transform; rifle.SetParent(holder, false); rifle.gameObject.AddComponent<Animator>();
            var muzzle = new GameObject("MuzzlePoint").transform; muzzle.SetParent(rifle, false); m.Gun.muzzlePoint = muzzle;
            m.Gun.ViewMotion.Initialize(m.Gun); m.Gun.GetComponent<WeaponVisuals>().Initialize(m.Gun);
            Assert(m.Gun.isActiveAndEnabled && !holder.gameObject.activeSelf && m.Gun.muzzlePoint != muzzle
                && m.Gun.muzzlePoint.IsChildOf(m.Gun.ViewMotion.MotionPivot), "Pistol disabled the gun or retained the rifle muzzle");
            m.Gun.ApplyLoadedGun("AK", 5, 27);
            Assert(m.Gun.isActiveAndEnabled && holder.gameObject.activeSelf && m.Gun.muzzlePoint == muzzle
                && m.Gun.currentAmmo == 5, "Rifle model switch lost the authored hierarchy or restored ammunition");
        });
        Test("Repair rewards stay capped across save continuation and renew next round", () => {
            var ledger = new BarricadeLedger(); int earned = 0;
            for (int i = 0; i < 20; i++) { ledger.Damage(0, 1); Assert(ledger.Repair(0, 1, out int reward), "Repair failed"); earned += reward; }
            Assert(earned == 120 && ledger.Count(0) == 6, "Unlimited repair farming");
            BarricadeLedger.TryDecode(ledger.Encode(), out var resumed); resumed.Damage(0, 1); resumed.Repair(0, 1, out int same);
            resumed.Damage(0, 1); resumed.Repair(0, 2, out int next);
            Assert(same == 0 && next == 10, "Continue reset reward cap or new round failed to reset it");
            Assert(!resumed.Damage(-1, 5) && !resumed.Repair(99, 1, out _), "Invalid window mutated ledger");
        });
        Test("Holding E repairs over time and respects pause range and one action per frame", () => {
            var m = new Match(); var world = m.MakeWorld();
            var entry = BarricadeEntry.Create(world.Barricades, SurvivalMapLayout.Breaches[0], m.Player.transform.position + Vector3.forward * 2);
            world.Barricades.Ledger.Damage(0, 3); entry.RefreshBoards(true); Input.KeysHeld.Add(KeyCode.E);
            Call(world, "Update"); Call(world, "Update");
            Assert(entry.Planks == 4 && m.Session.Points == 510, "Held repair was absent or repeated within one frame");
            Time.time = .4f; Time.frameCount++; Call(world, "Update"); Assert(entry.Planks == 4, "Repair ignored its interval");
            Time.time = 1; Time.frameCount++; Call(world, "Update"); Assert(entry.Planks == 5 && m.Session.Points == 520, "Held repair never advanced");
            Time.timeScale = 0; Time.time = 2; Time.frameCount++; Call(world, "Update"); Assert(entry.Planks == 5, "Paused repair changed boards");
            Time.timeScale = 1; entry.transform.position += Vector3.forward * 20; Time.frameCount++; Call(world, "Update");
            Assert(entry.Planks == 5 && m.Session.Points == 520, "Distant repair awarded points");
        });
        Test("Window attackers wait for their tell and release their reservation on death", () => {
            var m = new Match(); var world = m.MakeWorld();
            var entry = BarricadeEntry.Create(world.Barricades, SurvivalMapLayout.Breaches[0], m.Player.transform.position + Vector3.forward * 5);
            var zombie = Enemy(m, ZombieArchetype.Walker); zombie.transform.position = entry.transform.position - entry.transform.forward * 1.4f;
            Assert(zombie.BeginBreach(entry, entry.InnerPoint), "Could not reserve entry");
            Call(zombie, "Update"); Assert(entry.Planks == 6 && !zombie.GetComponent<NavMeshAgent>().enabled, "Entry ignored anticipation or navigation ownership");
            Time.time = .4f; Call(zombie, "Update"); Assert(entry.Planks == 5, "Attack never removed a board");
            Time.time = .5f; Call(zombie, "Update"); Assert(entry.Planks == 5, "Attack repeated without cooldown");
            zombie.TakeDamage(999); Assert(!zombie.IsEntering && entry.HasCapacity && !zombie.GetComponent<NavMeshAgent>().enabled, "Dead entry attacker retained a reservation or resumed navigation");
        });
        Test("A broken window vault hands movement back to the NavMesh after landing", () => {
            var m = new Match(); var world = m.MakeWorld();
            var entry = BarricadeEntry.Create(world.Barricades, SurvivalMapLayout.Breaches[0], m.Player.transform.position + Vector3.forward * 5);
            world.Barricades.Ledger.Damage(0, 6); var zombie = Enemy(m, ZombieArchetype.Runner);
            zombie.transform.position = entry.transform.position - entry.transform.forward * 1.4f; zombie.BeginBreach(entry, entry.InnerPoint);
            Call(zombie, "Update"); Time.time = 1.2f; Call(zombie, "Update");
            Assert(!zombie.IsEntering && zombie.GetComponent<NavMeshAgent>().enabled && zombie.transform.position == entry.InnerPoint, "Vault failed to restore navigation at its landing");
        });
        Test("Shared perimeter segments install eleven unique windows with six blocking boards", () => {
            var m = new Match(); var world = m.MakeWorld(); Call(world, "Start");
            Assert(world.Barricades.Entries.Count == 11 && world.Barricades.Entries.Select(e => e.Site.Id).Distinct().Count() == 11, "Shared fences duplicated or omitted entrances");
            foreach (var entry in world.Barricades.Entries)
            {
                Assert(entry.Planks == 6 && entry.GetComponent<NavMeshObstacle>().carving, "Window is not connected to board state/navigation");
                Assert(entry.GetComponentsInChildren<BoxCollider>().Count(c => c && c.enabled) >= 10, "Window lacks player sill/frame or board collision");
            }
        });
        Test("Optional shortcuts require the village and charge once while opening collision and navigation", () => {
            var m = new Match(); var world = m.MakeWorld(); Call(world, "Start"); m.Session.ApplySavedProgress(new GameState { points = 5000 });
            var gates = UnityEngine.Object.FindObjectsByType<WorldInteraction>(FindObjectsSortMode.None).Where(s => s.Kind == WorldInteraction.InteractionKind.Shortcut).ToArray();
            Assert(gates.Length == 2, "The alternative routes are missing"); var shortcut = gates[0]; shortcut.transform.position = m.Player.transform.position + Vector3.forward * 2;
            Assert(!world.TryActivate(shortcut) && m.Session.Points == 5000, "Shortcut bypassed closed districts");
            world.RestoreProgress(3, false); Time.frameCount++;
            Assert(world.TryActivate(shortcut) && m.Session.Points == 4350 && world.ShortcutFlags == shortcut.ShortcutBit, "Shortcut did not charge its fixed cost");
            Assert(!shortcut.GetComponentInParent<BoxCollider>().enabled && !shortcut.GetComponentInParent<NavMeshObstacle>().enabled
                && gates[1].GetComponentInParent<BoxCollider>().enabled, "Shortcut opened unrelated barriers or only its visual");
            Time.frameCount++; Assert(!world.TryActivate(shortcut) && m.Session.Points == 4350, "Open shortcut charged twice");
        });
        Test("Mystery chest enforces area access cooldown and a single fixed price", () => {
            var m = new Match(); m.Gun.InitNewGame(); var world = m.MakeWorld(); Call(world, "Start");
            m.Session.ApplySavedProgress(new GameState { points = 5000 });
            var station = UnityEngine.Object.FindObjectsByType<WorldInteraction>(FindObjectsSortMode.None).Single(s => s.Kind == WorldInteraction.InteractionKind.MysteryBox);
            station.transform.position = m.Player.transform.position + Vector3.forward * 2;
            Assert(!world.TryActivate(station) && m.Session.Points == 5000, "Closed area delivered a mystery weapon");
            world.RestoreProgress(3, false); Time.frameCount++; Assert(world.TryActivate(station) && m.Session.Points == 4050, "Chest failed its fixed-price transaction");
            Assert(m.Gun.Loadout.Get(0).Id == WeaponId.Pistol && m.Gun.Loadout.Get(1) != null, "Chest replaced the first weapon before filling slot two");
            Time.frameCount++; Assert(!world.TryActivate(station) && m.Session.Points == 4050, "Chest ignored delivery cooldown");
        });
        Test("SQLite continuation restores both weapons boards reward budget and shortcuts", () => {
            var m = new Match(); m.Gun.InitNewGame(); var world = m.MakeWorld(); Call(world, "Start");
            m.Session.ApplySavedProgress(new GameState { points = 5000 }); world.RestoreProgress(3, false, 5, 1);
            m.Gun.currentAmmo = 11; m.Gun.reserveAmmo = 22; m.Gun.PurchaseWeapon(WeaponId.SMG); m.Gun.currentAmmo = 21; m.Gun.reserveAmmo = 37;
            m.Rounds.currentRound = 5; world.Barricades.Ledger.Damage(0, 2); world.Barricades.Repair(world.Barricades.Entries.Single(e => e.Site.Id == 0));
            m.Saves.BeginSession(); m.Saves.FlushSave(); var snapshot = m.Db.LoadGame();
            var resumed = new Match(); var resumedWorld = resumed.MakeWorld(); resumed.Db.SaveGame(snapshot); PlayerPrefs.SetInt("load", 1);
            Call(new GameLoader { gunSystem = resumed.Gun }, "Start"); Call(resumedWorld, "Start");
            Assert(resumed.Gun.ActiveWeapon == WeaponId.SMG && resumed.Gun.currentAmmo == 21 && resumed.Gun.reserveAmmo == 37
                && resumed.Gun.Loadout.Get(0).Magazine == 11 && resumed.Gun.Loadout.Get(0).Reserve == 22, "Continue lost the inactive gun");
            Assert(resumedWorld.ShortcutFlags == 1 && resumedWorld.Barricades.Ledger.Count(0) == 5
                && resumedWorld.Barricades.Ledger.EarnedThisRound == 10, "Continue reset the barricade economy or shortcuts");
        });
    }
}
