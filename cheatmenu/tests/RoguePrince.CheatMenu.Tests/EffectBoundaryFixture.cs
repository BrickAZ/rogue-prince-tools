// Test-only models for the production effect adapter. Never compiled into the plugin.
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
internal sealed class AffectData
{internal IntPtr Pointer;internal object title=new();internal string name;internal int maxStacks=10;internal bool overridePreviousInstanceOnApply;}
internal sealed class AffectInstance {internal AffectData data;internal int stack,duration,total;}
internal sealed class AffectableComponent
{
    internal readonly List<AffectInstance> activeAffects=new();
    internal readonly List<string> Calls=new();
    internal bool Immune,BadDuration;
    internal bool CanBeAffectedBy(AffectData data,bool ignore)=>!Immune;
    internal bool HasAffect(AffectData data)=>activeAffects.Any(a=>a.data==data);
    internal int GetNbStacks(AffectData data)=>activeAffects.FirstOrDefault(a=>a.data==data)?.stack??0;
    internal int GetRemainingTime(AffectData data)=>activeAffects.FirstOrDefault(a=>a.data==data)?.duration??0;
    internal int GetTotalDuration(AffectData data)=>activeAffects.FirstOrDefault(a=>a.data==data)?.total??0;
    internal void RemoveStack(Entity source,AffectData data,int count){Calls.Add("RemoveStack");activeAffects.Single(a=>a.data==data).stack-=count;}
    internal void SetAffectData(AffectData data,Entity source,int duration,int stacks,OperationContext ctx,bool ignore)
    {Calls.Add("SetAffectData");var a=activeAffects.FirstOrDefault(a=>a.data==data);if(a==null){a=new(){data=data};activeAffects.Add(a);}a.stack=data.overridePreviousInstanceOnApply?stacks:a.stack+stacks;a.duration=a.total=duration;}
    internal void SetDuration(AffectData data,int duration,bool skip,bool total){Calls.Add("SetDuration");var a=activeAffects.Single(a=>a.data==data);a.duration=BadDuration?duration/2:duration;a.total=duration;}
    internal void RemoveAffect(Entity source,AffectData data){Calls.Add("RemoveAffect");activeAffects.RemoveAll(a=>a.data==data);}
}
internal sealed class OperationContext {internal static int Active;internal OperationContext(){Active++;}internal void Release(){Active--;}}
internal static class OperationContextUtils {internal static OperationContext CreateContextSourceAndTargetEntity(Entity source,Entity target)=>new();}
namespace MotherBase.ActionFramework {internal static class FrameInfo {internal static float FrameDuration=.01f;}}
namespace UnityEngine {internal static class Resources {internal static AffectData[] Effects=Array.Empty<AffectData>();internal static T[] FindObjectsOfTypeAll<T>()=>Effects.Cast<T>().ToArray();}}
namespace RoguePrince.CheatMenu
{
    internal sealed class GameSession
    {internal bool Ready=true,InventoryRecoveryBlocked;internal Entity Player=new(){Pointer=new(1)};internal PlayerSaver Saver;internal long Generation=1;internal IntPtr PlayerPointer=>Player.Pointer;internal AffectableComponent Affects=new();internal GameSession(){Saver=new(){mainCharacterVariable=new(){CurrentValue=Player}};}internal void RequireReady(){if(!Ready)throw new InvalidOperationException("not ready");}}
    internal static class TrainingActions {internal static int Room=1;internal static TrainingScope ReadScope(GameSession g)=>g.Ready?new(g.Generation,g.PlayerPointer.ToInt64(),1,Room,0,0):default;}
    internal sealed class ItemBrowser {internal sealed class Entry{internal ItemData Data;}internal readonly List<Entry> Items=new();internal void Reload(){}internal static LocalizedItemName ReadLabel(object title,string name)=>new(name,name,name);}
    internal static class Plugin {internal static readonly DamageTestLog MenuLog=new();internal static void LogEvent(string message){}}
    internal sealed class DamageTestLog{internal void LogWarning(string message){}}
}
internal static class EffectBoundaryTests
{
    internal static void Run(Action<string,Action> check)
    {
        void Equal(int a,int b){if(a!=b)throw new Exception($"{a} != {b}");}
        (RoguePrince.CheatMenu.GameSession,RoguePrince.CheatMenu.EffectActions,RoguePrince.CheatMenu.EffectActions.Entry) Setup(bool replace)
        {var g=new RoguePrince.CheatMenu.GameSession();var d=new AffectData{Pointer=new(10),name="effect",overridePreviousInstanceOnApply=replace};UnityEngine.Resources.Effects=new[]{d};var a=new RoguePrince.CheatMenu.EffectActions(g);a.Reload();g.Affects.activeAffects.Add(new(){data=d,stack=2,duration=10,total=10});return(g,a,a.Catalog.Single());}
        check("native replacing effect receives desired total stacks",()=>{var(g,a,e)=Setup(true);a.Apply(e,5,1);Equal(g.Affects.GetNbStacks(e.Data),5);Equal(g.Affects.GetRemainingTime(e.Data),100);Equal(OperationContext.Active,0);});
        check("native additive effect receives delta stacks",()=>{var(g,a,e)=Setup(false);a.Apply(e,5,1);Equal(g.Affects.GetNbStacks(e.Data),5);Equal(OperationContext.Active,0);});
        check("native reducing stacks invokes RemoveStack",()=>{var(g,a,e)=Setup(false);a.Apply(e,1,1);Equal(g.Affects.GetNbStacks(e.Data),1);if(!g.Affects.Calls.Contains("RemoveStack")||g.Affects.Calls.Contains("SetAffectData"))throw new Exception("wrong lifecycle");});
        check("native duration mismatch raises and releases pooled context",()=>{var(g,a,e)=Setup(false);g.Affects.BadDuration=true;try{a.Apply(e,5,1);throw new Exception("accepted");}catch(InvalidOperationException){}Equal(OperationContext.Active,0);});
        check("native immunity refuses before changing effect",()=>{var(g,a,e)=Setup(false);g.Affects.Immune=true;try{a.Apply(e,5,1);throw new Exception("accepted");}catch(InvalidOperationException){}Equal(g.Affects.GetNbStacks(e.Data),2);Equal(g.Affects.Calls.Count,0);});
        check("native clear invokes removal for finite captured effects",()=>{var(g,a,e)=Setup(false);a.Clear();Equal(g.Affects.activeAffects.Count,0);Equal(g.Affects.Calls.Count(c=>c=="RemoveAffect"),1);});
    }
}
