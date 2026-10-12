using RoguePrince.CheatMenu;
using RoguePrince.CheatMenu.Core;
using UnityEngine;

int passed=0,failed=0;
void Check(string name,Action work){try{Fixture.Reset();work();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
void Eq<T>(T actual,T expected){if(!Equals(actual,expected))throw new Exception($"expected {expected}, got {actual}");}
void Reject(Action work){try{work();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}

Check("capture real adapter includes enabled, disabled, player items and explicit gaps",()=>{
 var actions=new WorldReportActions(Fixture.game);actions.Capture();
 using var json=System.Text.Json.JsonDocument.Parse(actions.Json);
 Eq(json.RootElement.GetProperty("kind").GetString(),"scene-state-report");
 Eq(json.RootElement.GetProperty("canRestore").GetBoolean(),false);
 Eq(json.RootElement.GetProperty("synchronizedWorld").GetBoolean(),false);
 Eq(json.RootElement.GetProperty("entities").GetArrayLength(),3);
 Eq(json.RootElement.GetProperty("playerItems")[0].GetProperty("affixes")[0].GetString(),"affix-1");
 Eq(json.RootElement.GetProperty("uncovered").GetArrayLength()>4,true);
 Eq(Fixture.context._enabledEntities.Count,2);Eq(Fixture.player.GetComponent<Damageable>().currentHealth,5f);
});
Check("null native context rejected without clipboard writes",()=>{Fixture.game.Player.context=null;Reject(()=>new WorldReportActions(Fixture.game).Capture());Eq(GUIUtility.systemCopyBuffer,"sentinel");});
Check("dead player rejected",()=>{Fixture.game.Ready=false;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("loading rejected",()=>{Fixture.flow.isLoading=true;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("inactive run rejected",()=>{Fixture.flow.IsRunActive=false;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("unfocused capture rejected",()=>{Application.isFocused=false;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("running time rejected",()=>{Time.timeScale=1;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("wrong saver binding rejected",()=>{Fixture.game.Saver.mainCharacterVariable.CurrentValue=Fixture.disabled;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("same-count native entity mutation rejected",()=>{Fixture.context._enabledEntities.afterCopy=()=>Fixture.context._enabledEntities._version++;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("replaced native set rejected",()=>{Fixture.context._enabledEntities.afterCopy=()=>Fixture.context._enabledEntities=new(Fixture.player,Fixture.enemy);Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("wrong context member rejected",()=>{Fixture.enemy.context=new();Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("duplicate group member rejected",()=>{Fixture.context._disabledEntities=new(Fixture.player);Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("components version drift rejected",()=>{Fixture.player.componentCache.Values.afterCopy=()=>Fixture.player.componentCache._version++;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("item version drift rejected",()=>{Fixture.inventory.items.afterCopy=()=>Fixture.inventory.items._version++;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("run epoch changes during capture rejected",()=>{Fixture.inventory.items.afterCopy=()=>RunTools.RunId++;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("same-version inventory list replacement between stamp and read rejected",()=>{Fixture.context._enabledEntities.afterCopy=()=>Fixture.inventory.items=new(Fixture.inventory.items.values[0]);Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("frame advances during capture rejected",()=>{Fixture.inventory.items.afterCopy=()=>Time.frameCount++;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("nonfinite position rejected",()=>{Fixture.enemy.transform.position=new(float.NaN,0,0);Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("entity limit rejects before allocation",()=>{Fixture.context._enabledEntities.fakeCount=WorldReportLimits.Entities+1;Reject(()=>new WorldReportActions(Fixture.game).Capture());Eq(Fixture.context._enabledEntities.copyCalls,0);});
Check("late read error retires last report, copy never publishes partial report",()=>{var actions=new WorldReportActions(Fixture.game);actions.Capture();Fixture.enemy.transform.position=new(float.NaN,0,0);Reject(()=>actions.Capture());Eq(actions.HasReport,false);Reject(()=>actions.Copy());Eq(GUIUtility.systemCopyBuffer,"sentinel");});
Check("copy checks original sample scope before clipboard mutation",()=>{var actions=new WorldReportActions(Fixture.game);actions.Capture();Fixture.flow.seed++;Reject(()=>actions.Copy());Eq(GUIUtility.systemCopyBuffer,"sentinel");});
Check("copy exports captured report only",()=>{var actions=new WorldReportActions(Fixture.game);actions.Capture();actions.Copy();Eq(GUIUtility.systemCopyBuffer,actions.Json);Eq(Fixture.context._enabledEntities.Count,2);Eq(Fixture.player.GetComponent<Damageable>().currentHealth,5f);});
Check("clear drops managed report only",()=>{var actions=new WorldReportActions(Fixture.game);actions.Capture();actions.Clear();Eq(actions.HasReport,false);Eq(GUIUtility.systemCopyBuffer,"sentinel");Eq(Fixture.context._enabledEntities.Count,2);Eq(Fixture.player.GetComponent<Damageable>().currentHealth,5f);});
Check("closing menu retires managed report",()=>{var actions=new WorldReportActions(Fixture.game);actions.Capture();actions.Observe(false);Eq(actions.HasReport,false);});
Check("focus loss retires managed report",()=>{var actions=new WorldReportActions(Fixture.game);actions.Capture();Application.isFocused=false;actions.Observe(true);Eq(actions.HasReport,false);});
Check("map replacement retires managed report",()=>{var actions=new WorldReportActions(Fixture.game);actions.Capture();Fixture.flow.map=new();actions.Observe(true);Eq(actions.HasReport,false);});
Check("historical sample can be copied next frame in same scope",()=>{var actions=new WorldReportActions(Fixture.game);actions.Capture();Time.frameCount++;actions.Observe(true);actions.Copy();Eq(GUIUtility.systemCopyBuffer,actions.Json);});
Check("component limit rejects before publication",()=>{Fixture.enemy.componentCache=new(Enumerable.Range(0,WorldReportLimits.Components+1).Select(_=>new UnityEngine.Component()).ToArray());Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("item limit rejects before publication",()=>{Fixture.inventory.items=new(Enumerable.Range(0,WorldReportLimits.Items+1).Select(_=>new ItemInstanceData()).ToArray());Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("affix limit rejects before publication",()=>{Fixture.inventory.items.values[0].affix.affixesGuids=Enumerable.Range(0,WorldReportLimits.Affixes+1).Select(_=>new SerializationGuid()).ToArray();Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Check("nonfinite health rejected",()=>{Fixture.player.GetComponent<Damageable>().currentHealth=float.PositiveInfinity;Reject(()=>new WorldReportActions(Fixture.game).Capture());});
Console.WriteLine($"RESULT {passed} passed, {failed} failed");
return failed==0?0:1;
