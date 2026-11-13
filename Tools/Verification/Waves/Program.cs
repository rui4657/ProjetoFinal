using System;
using System.Reflection;
using UnityEngine;
static class Program {
 static int passed,failed;
 static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
 static void Call(object target,string name)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
 static RoundManager New(int active=24){Time.timeScale=1f;Time.deltaTime=1f;UnityEngine.Object.Registry.Clear();SurvivalSession.Instance=new SurvivalSession();var rm=new RoundManager{spawner=new ZombieSpawner(),maximumActiveZombies=active};Call(rm,"Start");return rm;}
 static void Start(RoundManager rm)=>Call(rm,"StartRound");
 static void Tick(RoundManager rm,int count=1){for(int i=0;i<count;i++)foreach(var c in rm.Coroutines.ToArray())c.Tick();}
 static void Test(string name,Action body){try{body();Console.WriteLine("PASS "+name);passed++;}catch(Exception e){Console.WriteLine("FAIL "+name+": "+(e.InnerException??e).Message);failed++;}}
 static int Main(){
 Test("Early first kill cannot finish an unfinished wave",()=>{var rm=New();Start(rm);Assert(rm.ZombiesAlive==1,"First zombie must spawn synchronously");rm.ZombieDied();Assert(rm.currentRound==1&&!rm.inIntermission&&rm.ZombiesRemaining==11,"Early kill skipped remaining spawns");Tick(rm);Assert(rm.spawner.Attempts==2&&rm.ZombiesAlive==1,"Wave did not continue");});
 Test("Final spawn, final death and coroutine completion finish exactly once",()=>{var rm=New();Start(rm);Tick(rm,11);Assert(rm.ZombiesAlive==12&&rm.IsSpawning,"Expected twelve active zombies before coroutine completes");for(int i=0;i<12;i++)rm.ZombieDied();Assert(rm.currentRound==1,"Wave finished before spawn coroutine");Tick(rm);Assert(rm.currentRound==2&&rm.inIntermission&&SurvivalSession.Instance.Completed.Count==1,"Wave completion not delivered once");rm.ZombieDied();rm.ZombieDied();Assert(rm.ZombiesAlive==0&&rm.currentRound==2&&SurvivalSession.Instance.Completed.Count==1,"Duplicate deaths changed wave state");});
 Test("Active enemy cap waits without dropping queued spawns",()=>{var rm=New(2);Start(rm);Tick(rm,3);Assert(rm.ZombiesAlive==2&&rm.spawner.Attempts==2&&rm.ZombiesRemaining==12,"Active cap failed");rm.ZombieDied();Tick(rm);Assert(rm.spawner.Attempts==3&&rm.ZombiesAlive==2&&rm.ZombiesRemaining==11,"Queued spawn did not resume");});
 Test("Repeated failed NavMesh spawns stop within configured attempts",()=>{var rm=New();rm.maximumSpawnFailures=3;for(int i=0;i<3;i++)rm.spawner.Outcomes.Enqueue(false);Start(rm);Tick(rm,10);Assert(rm.spawner.Attempts==3&&rm.HasSpawnError&&rm.IsStopped&&!rm.IsSpawning,"Spawn errors retried forever or advanced wave");Assert(rm.currentRound==1&&SurvivalSession.Instance.Completed.Count==0,"Failed wave granted progression");});
 Test("Missing spawn configuration blocks without free wave rewards",()=>{var rm=New();rm.spawner.IsConfigured=false;Start(rm);Assert(rm.IsStopped&&rm.HasSpawnError&&rm.spawner.Attempts==0,"Unconfigured spawner not blocked");Assert(SurvivalSession.Instance.Completed.Count==0,"Unconfigured wave awarded completion");});
 Test("Pause freezes preparation and pending spawns",()=>{var rm=New();float before=rm.intermissionTimer;Time.timeScale=0;Call(rm,"Update");Assert(rm.intermissionTimer==before,"Preparation timer ran during pause");Time.timeScale=1;Start(rm);int attempts=rm.spawner.Attempts;Time.timeScale=0;Tick(rm,4);Assert(rm.spawner.Attempts==attempts,"New zombies spawned while paused");Time.timeScale=1;Tick(rm);Assert(rm.spawner.Attempts==attempts+1,"Spawn failed to resume");});
 Test("Game over stops spawn coroutine and ignores late death callbacks",()=>{var rm=New();Start(rm);rm.StopWaves();int attempts=rm.spawner.Attempts;Tick(rm,20);rm.ZombieDied();Assert(rm.spawner.Attempts==attempts&&rm.IsStopped&&!rm.IsSpawning&&rm.currentRound==1,"Stopped waves continued");});
 Test("Preparation skip cannot revive a blocked spawner",()=>{var rm=New();rm.spawner.IsConfigured=false;Start(rm);rm.SkipIntermission();Call(rm,"Update");Assert(rm.spawner.Attempts==0&&rm.IsStopped,"Skip bypassed spawn failure");});
 Test("Wave size curve is bounded for corrupt or extreme save values",()=>{Assert(RoundManager.GetWaveSize(int.MaxValue,int.MaxValue)==250,"Integer overflow created invalid wave");Assert(RoundManager.GetWaveSize(1,5)==12,"First wave budget changed unexpectedly");Assert(RoundManager.GetWaveSize(1000,5)==250,"Late wave budget not bounded");Assert(RoundManager.GetWaveSize(-1,-4)>=1,"Negative save round invalid");});
 Console.WriteLine($"RESULT {passed} passed, {failed} failed");return failed==0?0:1;
 }
}
