static partial class Program
{
    static void ContentChecks()
    {
        Test("Loadout and barricade text use SQL parameters and preserve recovery data", () => {
            var db = new Database(); var state = State(); state.weapon_loadout = "v1;1;0,3,27;2,7,16";
            state.barricades = "v1;5;120;6,5,4,3,2,1,0,6,6,6,6"; state.shortcut_flags = 3;
            Assert(db.SaveGame(state), "Content write failed"); var loaded = db.LoadGame();
            Assert(loaded.weapon_loadout == state.weapon_loadout && loaded.barricades == state.barricades && loaded.shortcut_flags == 3, "Content snapshot changed");
            state.weapon_loadout = "'); DROP TABLE GameState; --"; db.SaveGame(state);
            Assert(db.HasSave() && db.LoadGame().weapon_loadout == state.weapon_loadout, "Content text executed SQL");
        });
        Test("Content schema migrates legacy AK saves without inventing a second weapon", () => {
            SQL("CREATE TABLE GameState(id INTEGER PRIMARY KEY,vida INTEGER,arma_nome TEXT,ammo_carregador INTEGER,ammo_reserva INTEGER);INSERT INTO GameState VALUES(1,80,'AK',9,77);");
            for (int i = 0; i < 2; i++)
            {
                var loaded = new Database().LoadGame();
                Assert(loaded.arma_nome == "AK" && loaded.ammo_carregador == 9 && loaded.ammo_reserva == 77
                    && loaded.weapon_loadout == "" && loaded.barricades == "" && loaded.shortcut_flags == 0, "Content migration reset or invented equipment");
            }
        });
        Test("Oversized content and invalid shortcut bits normalize without losing the run", () => {
            var db = new Database(); var state = State(); state.weapon_loadout = new string('x', 1000); state.barricades = new string('x', 1000); state.shortcut_flags = -1;
            Assert(db.SaveGame(state), "Invalid content blocked the recovery snapshot"); var loaded = db.LoadGame();
            Assert(loaded.vida == state.vida && loaded.weapon_loadout == "" && loaded.barricades == "" && loaded.shortcut_flags == 0, "Content bounds were not enforced");
        });
    }
}
