// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.EntityFramework;
using MotherBase.ActionFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UObject=UnityEngine.Object;
namespace RoguePrince.CheatMenu;
internal sealed class BossTargetActions
{
 internal static BossTargetActions Current;
 internal static bool Available;
 internal static bool AdditionalAttackAvailable;
 internal static bool ExtendedAvailable;
 internal sealed class Entry
 {
  internal Entity Owner;internal BossComponentBase Component;internal BSAIInput Input;
  internal TargetComponent Target;internal MoveIntent3D Move;internal Damageable Health;internal AIStateComponent Ai;
  internal IntPtr Brain;internal TrainingScope Scope;internal long Run;internal int Kind;
  internal string Name;
  internal string FilterLabel=>MenuText.Get(Kind==2?"仅新序列攻击":Kind is 3 or 4?"仅新攻击选择":Kind==5?"仅填充攻击":Kind>=0?"支持过滤":"尚未支持");
  internal string Explanation=>MenuText.Get(Kind switch
  {0=>"Nogai 过滤朝向、追赶与新攻击选择。阶段与已启动动作仍可能命中。",1=>"Kaali 过滤朝向及近战、远程行为选择。阶段与已启动动作仍可能命中。",2=>"Baatar 按实例过滤新序列攻击。阶段与已启动动作继续，仍可能命中。",3=>"Corrupted Nogai 仅过滤面向玩家的新攻击。阶段与已启动动作仍可能命中。",4=>"Altan 仅过滤常规新攻击选择。换位、外层特殊攻击仍可能命中。",5=>"Berude 仅过滤填充攻击。主攻击编排、追赶与朝向继续，仍可能命中。",_=>"阶段脑逻辑继续。动作自身追踪、场地攻击仍可能命中；其他 Boss 不支持。"});
 }
 private readonly GameSession game;
 private Entry bound;
 internal List<Entry> Entries {get;private set;}=new();
 internal bool Enabled {get;private set;}
 internal string ActiveName=>bound?.Name??MenuText.Get("未开启");
 internal string Status {get;private set;}="";
 internal BossTargetActions(GameSession game){this.game=game;Current=this;}
 private string Report(string text){Status=text;Plugin.LogEvent("boss_target "+text);return text;}
 private TrainingScope Scope()
 {
  var scope=TrainingActions.ReadScope(game);
  if((!Available&&!AdditionalAttackAvailable&&!ExtendedAvailable)||!game.Ready||!Application.isFocused||game.Player==null||game.Player.destroyed||!game.Player.gameObject.activeInHierarchy||!scope.Valid||
     scope.Player!=game.PlayerPointer.ToInt64()||scope.Scene!=game.Player.gameObject.scene.handle)return default;
  return scope;
 }
 private bool SameRoom(Entity owner,TrainingScope scope)
 {
  if(owner==null||owner.destroyed||!owner.gameObject.activeInHierarchy||owner.gameObject.scene.handle!=scope.Scene||owner.context==null||game.Player.context==null||
     owner.context.Pointer!=game.Player.context.Pointer||owner.GetComponent<MobComponent>()?.isBoss!=true)return false;
  var room=owner.GetComponent<RoomStateComponent>()?.currentRoom;
  if(room?.template==null)
  {var map=SceneActions.Map(SceneActions.Flow());if(map==null||!map.FindRoomAtCoord(owner.transform.position,out room))return false;}
  return MobRoomIdentity.Matches(game.Player.GetComponent<RoomStateComponent>()?.currentRoom,room);
 }
 private static int Kind(BossComponentBase component,BSAIInput input)
 {
  var brain=input?.brain;
  if(component?.TryCast<NogaiBossComponent>()!=null&&brain?.TryCast<NogaiBossBrain>()!=null)return 0;
  if(component?.TryCast<KaaliBossComponent>()!=null&&brain?.TryCast<KaaliBossBrain>()!=null)return 1;
  if(component?.TryCast<BaatarBossComponent>()!=null&&brain?.TryCast<BaatarBossBrain>()!=null)return 2;
  if(component?.TryCast<CorruptedNogaiBossComponent>()!=null&&brain?.TryCast<CorruptedNogaiBrain>()!=null)return 3;
  if(component?.TryCast<AltanBossComponent>()!=null&&brain?.TryCast<AltanBossBrain>()!=null)return 4;
  if(component?.TryCast<GoatBossComponent>()!=null&&brain?.TryCast<GoatBossBrain>()!=null)return 5;
  return -1;
 }
 internal static bool PatchReady(int kind)=>kind is 4 or 5?ExtendedAvailable:kind is 2 or 3?AdditionalAttackAvailable:kind is 0 or 1&&Available;
 private static BossComponentBase Component(Entity owner,int kind)=>kind switch
 {0=>owner.GetComponent<NogaiBossComponent>()?.TryCast<BossComponentBase>(),1=>owner.GetComponent<KaaliBossComponent>()?.TryCast<BossComponentBase>(),2=>owner.GetComponent<BaatarBossComponent>()?.TryCast<BossComponentBase>(),3=>owner.GetComponent<CorruptedNogaiBossComponent>()?.TryCast<BossComponentBase>(),4=>owner.GetComponent<AltanBossComponent>()?.TryCast<BossComponentBase>(),5=>owner.GetComponent<GoatBossComponent>()?.TryCast<BossComponentBase>(),_=>null};
 private bool Live(Entry entry)
 {
  var scope=Scope();if(entry==null||!PatchReady(entry.Kind)||!entry.Scope.Matches(scope)||entry.Run!=RunTools.RunId||!SameRoom(entry.Owner,scope))return false;
  var e=entry.Owner;var input=e.GetComponent<BSAIInput>();
  var component=Component(e,entry.Kind);
  var health=e.GetComponent<Damageable>();var ai=e.GetComponent<AIStateComponent>();
  return component?.Pointer==entry.Component?.Pointer&&component!=null&&component.brainInitDone&&component.currentPhaseIndex>0&&
   input?.Pointer==entry.Input?.Pointer&&input?.brain?.Pointer==entry.Brain&&Kind(component,input)==entry.Kind&&
   health?.Pointer==entry.Health?.Pointer&&health?.isAlive==true&&!health.onDieActionStarted&&
   ai?.Pointer==entry.Ai?.Pointer&&ai!=null&&!ai.stayIdle&&
   (entry.Kind==2||e.GetComponent<TargetComponent>()?.Pointer==entry.Target?.Pointer&&entry.Target!=null)&&
   (entry.Kind==3||e.GetComponent<MoveIntent3D>()?.Pointer==entry.Move?.Pointer&&entry.Move!=null)&&entry.Scope.Matches(Scope())&&entry.Run==RunTools.RunId;
 }
 internal string Refresh()
 {
  try
  {
   var scope=Scope();if(!scope.Valid)throw new InvalidOperationException(MenuText.Get("训练操作需要当前角色、房间与焦点就绪"));
   var entries=new List<Entry>();var run=RunTools.RunId;
   foreach(var component in UObject.FindObjectsOfType<BossComponentBase>())
   {
    var e=component?.GetComponent<Entity>();if(!SameRoom(e,scope))continue;
    var input=e.GetComponent<BSAIInput>();var health=e.GetComponent<Damageable>();
    if(health?.isAlive!=true||health.onDieActionStarted)continue;
    var kind=Kind(component,input);var name=kind switch{0=>"Nogai",1=>"Kaali",2=>"Baatar",3=>"Corrupted Nogai",4=>"Altan",5=>"Berude",_=>component.GetIl2CppType().Name};
    entries.Add(new(){Owner=e,Component=component,Input=input,Target=kind==2?null:e.GetComponent<TargetComponent>(),Move=e.GetComponent<MoveIntent3D>(),Health=health,Ai=e.GetComponent<AIStateComponent>(),Brain=input?.brain?.Pointer??IntPtr.Zero,Scope=scope,Run=run,Kind=kind,Name=name.Length>48?name[..48]:name});
   }
   if(!scope.Matches(Scope())||run!=RunTools.RunId)throw new InvalidOperationException(MenuText.Get("Boss 列表作用域已变化，请刷新"));
   Entries=entries.GroupBy(x=>x.Owner.Pointer).Select(x=>x.First()).ToList();
   return Report(MenuText.Format("当前房间 Boss：{0}；支持 Nogai / Kaali / Baatar / Corrupted Nogai / Altan / Berude",Entries.Count));
  }
  catch(Exception e){Entries.Clear();Disable(e);throw;}
 }
 internal string SetEnabled(bool enabled,Entry entry=null)
 {
  if(!enabled){Clear();return Report(MenuText.Get("Boss 目标行为过滤已关闭"));}
  if(!Available&&!AdditionalAttackAvailable&&!ExtendedAvailable)throw new InvalidOperationException(MenuText.Get("原生补丁不可用"));
  if(entry==null||!Entries.Contains(entry))throw new InvalidOperationException(MenuText.Get("请刷新并选择当前房间 Boss"));
  if(entry.Kind<0)throw new InvalidOperationException(MenuText.Get("该 Boss 尚不支持目标行为过滤；未执行"));
  if(!PatchReady(entry.Kind))throw new InvalidOperationException(MenuText.Get("原生补丁不可用"));
  if(!Live(entry))throw new InvalidOperationException(MenuText.Get("Boss 或原生组件未就绪，请刷新"));
  bound=entry;Enabled=true;return Report(MenuText.Format("已过滤 {0}：{1}；已启动动作继续",entry.Name,entry.FilterLabel));
 }
 internal bool Block(Entity owner,IntPtr brain,int kind,BossComponentBase component=null,Entity explicitTarget=null,TargetComponent target=null,MoveIntent3D move=null,bool stopMovement=false,AIStateComponent ai=null)
 {
  if(!Enabled)return false;
  if(owner?.Pointer!=bound.Owner.Pointer||brain!=bound.Brain||kind!=bound.Kind)return false;
  try
  {
   if(!Live(bound)){Retire();return false;}
   if(component!=null&&component.Pointer!=bound.Component.Pointer||explicitTarget!=null&&explicitTarget.Pointer!=game.PlayerPointer||
      target!=null&&target.Pointer!=bound.Target?.Pointer||move!=null&&move.Pointer!=bound.Move.Pointer||ai!=null&&ai.Pointer!=bound.Ai.Pointer||kind!=2&&bound.Target.target?.Pointer!=game.PlayerPointer)return false;
   if(stopMovement)bound.Move.magnitude=0;
   return true;
  }
  catch(Exception e){Disable(e);return false;}
 }
 private void Retire(){Clear();Report(MenuText.Get("Boss 过滤已关闭：目标或作用域失效"));}
 internal void Tick(){if(!Enabled)return;try{if(!Live(bound))Retire();}catch(Exception e){Disable(e);}}
 internal void Clear(){Enabled=false;bound=null;Status=MenuText.Get("Boss 目标行为过滤已关闭");}
 internal void Disable(Exception e){Clear();Status=MenuText.Get("Boss 过滤读取失败，已关闭；可重新开启");Plugin.MenuLog.LogWarning("[RPCheat] boss_target_disabled "+e);}
}
