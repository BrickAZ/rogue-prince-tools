using RoguePrince.CheatMenu;
using MotherBase.ActionFramework;
static class GraphTests
{
 internal static void Run(Action<bool,string> check)
 {
  var variable=new RoomVariable();var feature=new TestRoomFeature{roomVariable=variable,_active=true};
  var camera=new BossSetupCameraFeature{_active=true};
  var action=new ActionData{actionFeatures=new()};action.actionFeatures.Add(feature);action.actionFeatures.Add(camera);feature.next=action;
  var entry=new BossAiAction{attackAction=action};var brain=new AltanBossBrain{cornerActions=new()};brain.cornerActions.Add(entry);
  var v1=new RoomVariable();var v2=new RoomVariable();
  using var g1=new BossSpawnAssetGraph(t=>t==typeof(RoomVariable)?v1:null!);
  using var g2=new BossSpawnAssetGraph(t=>t==typeof(RoomVariable)?v2:null!);
  var b1=g1.Clone(brain);var b2=g2.Clone(brain);
  check(b1.Pointer!=brain.Pointer&&b2.Pointer!=b1.Pointer,"two private native brains");
  check(b1.cornerActions!.Pointer!=brain.cornerActions.Pointer,"private native list");
  check(b1.cornerActions[0].Pointer!=entry.Pointer,"private serialized attack entry");
  var a1=(ActionData)b1.cornerActions[0].attackAction!;var a2=(ActionData)b2.cornerActions![0].attackAction!;
  check(a1.Pointer!=action.Pointer&&a2.Pointer!=a1.Pointer,"two private native actions");
  var f1=(TestRoomFeature)a1.actionFeatures![0];var f2=(TestRoomFeature)a2.actionFeatures![0];
  check(f1.Pointer!=feature.Pointer&&f1.roomVariable!.Pointer==v1.Pointer&&f2.roomVariable!.Pointer==v2.Pointer,"per-instance room service");
  check(f1.next!.Pointer==a1.Pointer,"cyclic native action graph terminates and stays private");
  check(feature.roomVariable!.Pointer==variable.Pointer&&feature.next!.Pointer==action.Pointer,"source feature untouched");
  check(!a1.actionFeatures[1]._active&&camera._active,"camera disabled only on private feature");
  f1._active=false;check(feature._active&&f2._active,"one summon cannot mutate source or another summon");
  var config=new ActorConfig{startActions=new(1L)};config.startActions[0]=action;
  var c=g1.Clone(config);check(c.startActions!.Pointer!=config.startActions.Pointer&&c.startActions[0].Pointer==a1.Pointer,"native array uses its actual index type and shares owned clone");
  var bindings=new ActionBindings{actionBindings=new()};bindings.actionBindings.Add(new ActionBinding{action=action});
  var bindingCopy=g1.Clone(bindings);check(bindingCopy.actionBindings![0].Pointer!=bindings.actionBindings[0].Pointer&&bindingCopy.actionBindings[0].action!.Pointer==a1.Pointer,"private binding entries route to private action");
  var goatEntry=new GoatAiAction{attackAction=action};var goat=new GoatBossBrain{aiActions=new(1L)};goat.aiActions[0]=goatEntry;
  var goatCopy=g1.Clone(goat);
  check(goatCopy.aiActions!.Pointer!=goat.aiActions.Pointer&&goatCopy.aiActions[0].Pointer!=goatEntry.Pointer&&goatCopy.aiActions[0].attackAction!.Pointer==a1.Pointer,"derived GoatAiAction array rebinds the private action graph");
  var launch=new LaunchActionEffectData{toLaunch=action};var effect=new AtFrameEffect{effect=new EffectDataRef{data=launch}};
  var effectCopy=g1.Clone(effect);
  check(effectCopy.effect!.Pointer!=effect.effect.Pointer&&effectCopy.effect.data!.Pointer!=launch.Pointer&&((LaunchActionEffectData)effectCopy.effect.data).toLaunch!.Pointer==a1.Pointer,"managed-reference effect launches owned action without sharing mutable state");
  check(launch.toLaunch!.Pointer==action.Pointer&&goatEntry.attackAction!.Pointer==action.Pointer,"derived entries and nested effect source remain untouched");
  var globalBoss=new EntityVariable{name="BossEntityVariable"};var privateBoss=new EntityVariable();
  var selector=new AtFrameEffect{effect=new EffectDataRef{data=new VariableEffect{variable=globalBoss}}};
  using var isolated=new BossSpawnAssetGraph(t=>null!,privateBoss);
  var selectorCopy=isolated.Clone(selector);
  check(((VariableEffect)selectorCopy.effect!.data!).variable!.Pointer==privateBoss.Pointer,"Boss selector isolation also works without a prefab publisher");
  check(((VariableEffect)selector.effect!.data!).variable!.Pointer==globalBoss.Pointer,"formal encounter variable is untouched");
  var playerVariable=new EntityVariable{name="MainCharacterVariable"};((VariableEffect)selector.effect.data).variable=playerVariable;
  using var playerGraph=new BossSpawnAssetGraph(t=>null!,privateBoss);
  check(((VariableEffect)playerGraph.Clone(selector).effect!.data!).variable!.Pointer==playerVariable.Pointer,"player variable remains the native read-only player source");
  var eventSource=new SendUIEventEffectData{uiEvent=new UIEventID{streamCategory="CoruptedNogaiRoom",streamName="CollapseBuilding01"}};
  var broadcast=new AtFrameEffect{effect=new EffectDataRef{data=eventSource}};var privateEvent=(SendUIEventEffectData)isolated.Clone(broadcast).effect!.data!;
  check(privateEvent.uiEvent.streamCategory.StartsWith("RPCheat-Boss-")&&eventSource.uiEvent.streamCategory=="CoruptedNogaiRoom","arena collapse broadcasts use a private stream without modifying the real encounter");
  var walls=new BackWallConfiguration{elements=new()};walls.elements.Add(new BackWallElement{x=27.5f});var wallsCopy=g1.Clone(walls);
  check(wallsCopy.Pointer!=walls.Pointer&&wallsCopy.elements!.Pointer!=walls.elements.Pointer&&wallsCopy.elements[0].Pointer!=walls.elements[0].Pointer,"wall configuration and native elements are owned copies");
  wallsCopy.elements![0].x=3;check(walls.elements[0].x==27.5f,"mapping walls cannot alter formal arena coordinates");
  var count=g1.Count;var before=UnityEngine.Object.Destroyed.Count;g1.Dispose();
  check(UnityEngine.Object.Destroyed.Count-before==count,"dispose every owned asset once");
  g1.Dispose();check(UnityEngine.Object.Destroyed.Count-before==count,"idempotent asset cleanup");
 }
}
