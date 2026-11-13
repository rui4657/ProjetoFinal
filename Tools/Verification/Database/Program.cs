using System;
using System.Data;
using System.Globalization;
using System.IO;
using Mono.Data.SqliteClient;
using UnityEngine;
static class Program {
 static int passed,failed;
 static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
 static void NewPath(){Application.persistentDataPath=Path.Combine(Environment.GetEnvironmentVariable("ZOMBIES_TEST_DATA") ?? Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Application.persistentDataPath);}
 static void SQL(string query){using(var c=new SqliteConnection("URI=file:"+Path.Combine(Application.persistentDataPath,"savegame.db"))){c.Open();using(IDbCommand cmd=c.CreateCommand()){cmd.CommandText=query;cmd.ExecuteNonQuery();}}}
 static long Scalar(string query){using(var c=new SqliteConnection("URI=file:"+Path.Combine(Application.persistentDataPath,"savegame.db"))){c.Open();using(IDbCommand cmd=c.CreateCommand()){cmd.CommandText=query;return Convert.ToInt64(cmd.ExecuteScalar());}}}
 static GameState State()=>new GameState{id=1,vida=90,arma_nome="AK",ammo_carregador=17,ammo_reserva=87,round=8,pos_x=1.25f,pos_y=2.5f,pos_z=-3.125f,has_position=1,points=2345,kills=20,headshots=5,weapon_tier=2,health_bonus=50,reload_perk=1};
 static void Test(string name,Action body){try{NewPath();body();Console.WriteLine("PASS "+name);passed++;}catch(Exception e){Console.WriteLine("FAIL "+name+": "+(e.InnerException??e));failed++;}}
 static int Main(){
 Test("Fresh database creates schema without creating a save",()=>{var db=new Database();Assert(db.IsAvailable,"Database unavailable");Assert(!db.HasSave()&&db.LoadGame()==null,"Inspection created save data");Assert(Scalar("SELECT COUNT(*) FROM pragma_table_info('GameState')")==16,"Schema lacks current fields");});
 Test("Full snapshot roundtrip preserves combat and progression",()=>{var db=new Database();var s=State();Assert(db.SaveGame(s),"Write failed");var got=db.LoadGame();Assert(got.vida==90&&got.ammo_carregador==17&&got.round==8&&got.pos_z==-3.125f&&got.points==2345&&got.weapon_tier==2&&got.health_bonus==50&&got.reload_perk==1,"Snapshot changed");});
 Test("Legacy nine-column save migrates preserving position and progress",()=>{SQL("CREATE TABLE GameState(id INTEGER PRIMARY KEY,vida INTEGER,arma_nome TEXT,ammo_carregador INTEGER,ammo_reserva INTEGER,round INTEGER,pos_x REAL,pos_y REAL,pos_z REAL);INSERT INTO GameState VALUES(1,75,'AK',8,60,4,10.5,2,-7.25);");var db=new Database();var got=db.LoadGame();Assert(db.IsAvailable&&got!=null,"Migration lost save");Assert(got.vida==75&&got.round==4&&got.pos_x==10.5f&&got.has_position==1,"Legacy values changed");Assert(got.points==500&&got.weapon_tier==0,"Legacy progression defaults wrong");});
 Test("Oldest five-column save migrates and schema upgrades are idempotent",()=>{SQL("CREATE TABLE GameState(id INTEGER PRIMARY KEY,vida INTEGER,arma_nome TEXT,ammo_carregador INTEGER,ammo_reserva INTEGER);INSERT INTO GameState VALUES(1,80,'AK',10,30);");for(int i=0;i<3;i++){var db=new Database();Assert(db.IsAvailable&&db.LoadGame().vida==80,"Repeated migration failed");}Assert(Scalar("SELECT COUNT(*) FROM pragma_table_info('GameState')")==16,"Migration duplicated or missed columns");});
 Test("Parameterized weapon names cannot execute SQL",()=>{var db=new Database();var s=State();s.arma_nome="AK'); DROP TABLE GameState; --";Assert(db.SaveGame(s),"Parameterized name failed");Assert(db.LoadGame().arma_nome==s.arma_nome&&db.HasSave(),"SQL injection corrupted database");});
 Test("Portuguese culture preserves decimal positions",()=>{var previous=CultureInfo.CurrentCulture;try{CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("pt-PT");var db=new Database();Assert(db.SaveGame(State()),"Culture write failed");Assert(db.LoadGame().pos_x==1.25f&&db.LoadGame().pos_z==-3.125f,"Decimal coordinates changed");}finally{CultureInfo.CurrentCulture=previous;}});
 Test("Dead snapshot cannot overwrite living recovery state",()=>{var db=new Database();var s=State();Assert(db.SaveGame(s),"Initial save failed");s.vida=0;Assert(!db.SaveGame(s),"Dead state was written");Assert(db.LoadGame().vida==90,"Living save overwritten by death");});
 Test("Deleting another slot does not remove active recovery slot",()=>{var db=new Database();db.SaveGame(State());Assert(db.DeleteSave(2)&&db.HasSave(),"Other slot deletion removed slot1");Assert(db.DeleteSave()&&!db.HasSave()&&db.LoadGame()==null,"Recovery slot deletion failed");});
 Test("Corrupt database reports unavailability without throwing",()=>{File.WriteAllText(Path.Combine(Application.persistentDataPath,"savegame.db"),"this is not sqlite");var db=new Database();Assert(!db.IsAvailable&&db.LoadGame()==null&&!db.SaveGame(State()),"Corrupt database was accepted");});
 Console.WriteLine($"RESULT {passed} passed, {failed} failed");return failed==0?0:1;
 }
}
