// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using HarmonyLib;
using MotherBase.ActionFramework;
using MotherBase.EntityFramework;
using MotherBase.ApplicationFlowManager;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;
internal sealed class CombatActions
{
    internal static CombatActions Current;
    internal static bool DamageAvailable,MotionAvailable,AttackAvailable,ToolAvailable,PracticeAvailable;
    private readonly GameSession game;
    internal GameSession Game=>game;
    internal readonly ToolLimitActions Tools;
    internal readonly CombatLedger Ledger=new();
    internal readonly PracticeStatistics Practice=new();
    internal float DamageMultiplier=1,MoveMultiplier=1,AttackMultiplier=1;
    internal bool OneHit,Undying,NoKnockback,InfiniteJump,ToolCooldown,ToolNoCost,Recording,AutoPractice;
    internal bool WaveRecording;
    internal IntPtr Dummy;
    internal float LastMovementSpeed;
    private TrainingScope practiceScope,modeScope;
    private Vector3 lastPosition;
    private bool practiceHadActive,observedDeath;
    private ApplicationFlowManager exitFlow;
    private string exitTarget;
    private PracticeRecord pendingExit;
    private float exitDeadline;
    internal bool Active=>Tools.UsesLocked||Recording||AutoPractice||WaveRecording||pendingExit!=null||DamageMultiplier!=1||MoveMultiplier!=1||AttackMultiplier!=1||OneHit||Undying||NoKnockback||InfiniteJump||ToolCooldown||ToolNoCost||Dummy!=IntPtr.Zero;
    internal CombatActions(GameSession game){this.game=game;Tools=new(this);Current=this;}
    internal bool Bound=>game.Player!=null&&!game.Player.destroyed&&game.Saver?.mainCharacterVariable?.CurrentValue?.Pointer==game.PlayerPointer;
    internal bool IsPlayer(Entity entity)=>Bound&&entity?.Pointer==game.PlayerPointer;
    internal long Generation=>game.Generation;
    internal bool ModesBound=>Bound&&Application.isFocused&&modeScope.Matches(TrainingActions.ReadScope(game));
    internal void Require(){game.RequireReady();var scope=TrainingActions.ReadScope(game);if(!scope.Valid)throw new InvalidOperationException(MenuText.Get("当前战斗作用域未就绪"));modeScope=scope;}
    internal void SetDamage(float value){Require();DamageMultiplier=CombatPolicy.Multiplier(value);}
    internal void SetMovement(float value){Require();MoveMultiplier=CombatPolicy.Multiplier(value,.25f,3);}
    internal void SetAttack(float value){Require();AttackMultiplier=CombatPolicy.Multiplier(value,.25f,3);}
    internal HashSet<IntPtr> WeaponActions(bool toolOnly)
    {
        var result=new HashSet<IntPtr>();if(!Bound)return result;
        var inv=game.Player.GetComponent<InventoryComponent>();if(inv==null)return result;
        void Add(ItemInstanceData item)
        {var bindings=item?.source?.GetComponent<WeaponFeature>()?.attackBindings;if(bindings==null)return;for(int i=0;i<bindings.Length;i++){var action=bindings[i]?.actionTrigger?.GetActionData(game.Player);if(action!=null)result.Add(action.Pointer);}}
        if(!toolOnly&&inv.TryGetMainWeapon(out var main))Add(main);
        if(inv.TryGetSecondaryWeapon(out var tool)&&tool?.source?.HasComponent<UtilityWeaponFeature>()==true)Add(tool);
        return result;
    }
    internal List<CooldownID> ToolIds()
    {
        var ids=new Dictionary<IntPtr,CooldownID>();
        var inv=game.Player.GetComponent<InventoryComponent>();if(inv==null||!inv.TryGetSecondaryWeapon(out var tool))return new();
        var bindings=tool?.source?.GetComponent<WeaponFeature>()?.attackBindings;if(bindings==null)return new();
        for(int i=0;i<bindings.Length;i++)
        {var features=bindings[i]?.actionTrigger?.GetActionData(game.Player)?.actionFeatures;if(features==null)continue;for(int j=0;j<features.Count;j++){var list=features[j]?.TryCast<CheckCooldownFeature>()?.cooldownToCheck;if(list==null)continue;for(int k=0;k<list.Count;k++)if(list[k]!=null)ids[list[k].Pointer]=list[k];}}
        return ids.Values.ToList();
    }
    internal string ResetTool()
    {
        Require();var ids=ToolIds();var component=game.Player.GetComponent<CooldownComponent>();
        if(component==null||ids.Count==0)throw new InvalidOperationException(MenuText.Get("当前工具没有可识别的原生冷却"));
        foreach(var id in ids)if(component.Has(id))component.RemoveCooldown(id,game.Player);
        if(ids.Any(id=>component.Has(id)))throw new InvalidOperationException(MenuText.Get("工具冷却回读失败"));
        return MenuText.Get("当前工具冷却已重置");
    }
    internal bool CanBypassToolCost(Entity entity,ActionData action)
    {
        if(!ToolNoCost||!ModesBound||!IsPlayer(entity)||action==null)return false;
        return CombatPolicy.ToolCostBypass(ToolNoCost,ModesBound,IsPlayer(entity),WeaponActions(true).Contains(action.Pointer),entity.GetComponent<EnergyComponent>()?.isInitialized==true);
    }
    internal void SetToolNoCost(bool enabled)
    {
        if(enabled)
        {
            Require();var ids=WeaponActions(true);var inv=game.Player.GetComponent<InventoryComponent>();
            var found=false;
            if(inv!=null&&inv.TryGetSecondaryWeapon(out var tool))
            {var bindings=tool?.source?.GetComponent<WeaponFeature>()?.attackBindings;if(bindings!=null)for(int i=0;i<bindings.Length;i++){var data=bindings[i]?.actionTrigger?.GetActionData(game.Player);if(data==null||!ids.Contains(data.Pointer))continue;var features=data.actionFeatures;if(features!=null)for(int j=0;j<features.Count;j++)if(features[j]?.TryCast<ConsumeEnergySlotActionFeature>()!=null)found=true;}}
            if(!found||game.Player.GetComponent<EnergyComponent>()?.isInitialized!=true)throw new InvalidOperationException(MenuText.Get("当前工具没有就绪的原生能量槽消耗"));
        }
        ToolNoCost=enabled;
    }
    internal void HitPlayer(){Practice.Hit();}
    internal void MarkPractice(string reason){Practice.Finish(reason,false,Position());practiceScope=default;pendingExit=null;}
    internal void NativeExit(ApplicationFlowManager flow,ApplicationSignal signal)
    {
        if(!AutoPractice||WaveRecording||!Practice.Running||!Bound||flow==null||flow.isLoading||signal==null||!TrainingActions.ReadScope(game).Valid)return;
        if(!(signal.name=="NextBiome"||signal.name.StartsWith("BiomeChoice",StringComparison.Ordinal))||!flow.AcceptsSignal(signal))return;
        var next=flow.GetNextState(signal);if(next==null||next.guid==flow.GetCurrentState()?.guid)return;
        var actual=RuntimeServiceLocator.Get<ApplicationFlowService>()?.GetCurrentStateLogic()?.flowManagerVariable?.CurrentValue;
        if(actual?.Pointer!=flow.Pointer)return;
        pendingExit=Practice.Snapshot("native exit pending",Position());exitFlow=flow;exitTarget=next.guid;exitDeadline=Time.realtimeSinceStartup+45;
        Practice.Finish("native exit pending",false,Position());Plugin.LogEvent("practice_exit_pending target="+exitTarget);
    }
    private void ConfirmNativeExit()
    {
        if(pendingExit==null)return;
        if(exitFlow!=null&&!exitFlow.isLoading&&exitFlow.GetCurrentState()?.guid==exitTarget){Practice.Confirm(pendingExit);Plugin.LogEvent("practice_exit_confirmed");pendingExit=null;}
        else if(Time.realtimeSinceStartup>exitDeadline){Plugin.LogEvent("practice_exit_timeout");pendingExit=null;}
    }
    private string Position()=>game.Player==null?"":game.Player.transform.position.ToString();
    internal void Tick(bool menuVisible,bool loading,bool busy)
    {
        ConfirmNativeExit();
        var scope=TrainingActions.ReadScope(game);
        if(!modeScope.Matches(scope)||!Application.isFocused)StopModes();
        Tools.Tick();
        var active=scope.Valid&&Application.isFocused&&!menuVisible&&!loading&&!busy&&Time.timeScale>0;
        Ledger.Advance(Time.unscaledDeltaTime,Recording&&active&&practiceHadActive);
        if(AutoPractice||WaveRecording)
        {
            if(!game.Ready&&Bound&&!observedDeath){Practice.Finish("death",false,Position());observedDeath=true;}
            if(!scope.Valid){if(Practice.Running&&!observedDeath)Practice.Finish("loading",false,Position());practiceScope=default;}
            else
            {
                observedDeath=false;
                if(!WaveRecording&&!practiceScope.Matches(scope))
                {if(Practice.Running)Practice.Finish("room change",false,Position());practiceScope=scope;var room=game.Player.GetComponent<RoomStateComponent>()?.currentRoom;Practice.Start((room?.template?.originRoomData?.name??"room")+$" ({scope.RoomX},{scope.RoomY})",0);}
                else if(!WaveRecording&&Practice.Running&&Vector3.Distance(game.Player.transform.position,lastPosition)>8)MarkPractice("teleport");
                Practice.Advance(Time.unscaledDeltaTime,active&&practiceHadActive);
                lastPosition=game.Player.transform.position;
            }
        }
        practiceHadActive=active;
    }
    internal void StopModes(){Tools.Clear();DamageMultiplier=MoveMultiplier=AttackMultiplier=1;OneHit=Undying=NoKnockback=InfiniteJump=ToolCooldown=ToolNoCost=false;Dummy=IntPtr.Zero;}
    internal void Reset(bool rebind=false){if(pendingExit!=null&&(rebind||exitFlow?.isLoading==true)){Practice.Finish("loading",false,Position());practiceScope=default;}else{MarkPractice(game.Ready?"reset":"death");AutoPractice=false;}StopModes();Recording=false;WaveRecording=false;practiceHadActive=false;}
    internal static void HookError(Exception e){Current?.StopModes();Plugin.MenuLog.LogWarning("[RPCheat] combat_hook_disabled "+e);}
}
[HarmonyPatch(typeof(Damageable),nameof(Damageable.ApplyDamages))]
internal static class CombatDamagePatch
{
    private static void Prefix(Damageable __instance,DamageSystem.Connection __1,Entity __2,out DamageHooks.Operation __state){__state=null;try{DamageHooks.Begin(__instance,__1,__2,out __state);}catch(Exception e){CombatActions.HookError(e);}}
    private static Exception Finalizer(DamageHooks.Operation __state,Exception __exception){try{DamageHooks.End(__state,__exception);}catch(Exception e){CombatActions.HookError(e);}return __exception;}
}
[HarmonyPatch(typeof(Damageable),"set_currentHealth")]
internal static class CombatHealthPatch
{
    private static void Prefix(Damageable __instance,ref float __0)
    {try{var c=CombatActions.Current;var stack=DamageHooks.Stack;if(c==null||!c.ModesBound||stack?.Count>0!=true)return;var op=stack.Peek();if(op.Health.Pointer!=__instance.Pointer||op.Generation!=c.Generation)return;__0=CombatPolicy.Health(__instance.currentHealth,__0,op.Outgoing?c.DamageMultiplier:1,op.Outgoing&&c.OneHit,op.Player&&c.Undying||op.Target==c.Dummy,op.CannotKill);}catch(Exception e){CombatActions.HookError(e);}}
}
[HarmonyPatch(typeof(Damageable),nameof(Damageable.Kill))]
internal static class CombatKillPatch
{
    private static bool Prefix(Damageable __instance){try{var c=CombatActions.Current;if(c?.ModesBound==true&&((c.Undying&&c.IsPlayer(__instance._entity))||c.Dummy==__instance._entity?.Pointer)){if(__instance.currentHealth<1)__instance.currentHealth=1;return false;}}catch(Exception e){CombatActions.HookError(e);}return true;}
}
[HarmonyPatch(typeof(DamageSystem),nameof(DamageSystem.ApplyBump))]
internal static class CombatBumpPatch
{
    private static bool Prefix(Entity __0){try{var c=CombatActions.Current;return !(c?.ModesBound==true&&(c.NoKnockback&&c.IsPlayer(__0)||c.Dummy!=IntPtr.Zero&&c.Dummy==__0?.Pointer));}catch(Exception e){CombatActions.HookError(e);return true;}}
}
[HarmonyPatch(typeof(JumpTrigger),nameof(JumpTrigger.GetActionData))]
internal static class CombatJumpTriggerPatch
{
    private static void Postfix(JumpTrigger __instance,Entity __0,ref ActionData __result){try{var c=CombatActions.Current;if(__result==null&&c?.InfiniteJump==true&&c.ModesBound&&c.IsPlayer(__0))__result=__instance.groundJumpNoSpeed??__instance.groundJump;}catch(Exception e){CombatActions.HookError(e);}}
}
[HarmonyPatch(typeof(JumpFeature),nameof(JumpFeature.CanStart))]
internal static class CombatJumpPatch
{
    private static void Postfix(Entity __0,ref bool __result){try{var c=CombatActions.Current;if(c?.InfiniteJump==true&&c.ModesBound&&c.IsPlayer(__0))__result=true;}catch(Exception e){CombatActions.HookError(e);}}
}
internal sealed class MovementScope {internal bool Player;internal MovementScope Previous;}
[HarmonyPatch(typeof(CharacterMovementFeature),nameof(CharacterMovementFeature.MovementUpdate))]
internal static class CombatMovementScopePatch
{
    [ThreadStatic] internal static MovementScope Scope;
    private static void Prefix(Entity __0,out MovementScope __state){__state=Scope;Scope=new(){Player=CombatActions.Current?.IsPlayer(__0)==true,Previous=Scope};}
    private static Exception Finalizer(MovementScope __state,Exception __exception){Scope=__state;return __exception;}
}
[HarmonyPatch(typeof(CharacterMovementFeature),nameof(CharacterMovementFeature.GetMovementSpeed))]
internal static class CombatMovePatch
{
    private static void Postfix(ref float __result){try{var c=CombatActions.Current;if(c?.ModesBound!=true||CombatMovementScopePatch.Scope?.Player!=true)return;if(float.IsFinite(__result)){__result*=c.MoveMultiplier;c.LastMovementSpeed=__result;}}catch(Exception e){CombatActions.HookError(e);}}
}
internal sealed class ActorStepScope {internal Actor Actor;internal HashSet<IntPtr> Attacks;internal bool First=true;internal ActionStepPlan Plan;}
[HarmonyPatch(typeof(Actor),nameof(Actor.UpdateState))]
internal static class CombatActorPatch
{
    [ThreadStatic] internal static ActorStepScope Scope;
    private static void Prefix(Actor __instance,Entity __0,out ActorStepScope __state)
    {__state=Scope;try{var c=CombatActions.Current;Scope=c?.IsPlayer(__0)==true&&c.AttackMultiplier!=1&&c.ModesBound?new(){Actor=__instance,Attacks=c.WeaponActions(false)}:null;}catch(Exception e){Scope=null;CombatActions.HookError(e);}}
    private static Exception Finalizer(ActorStepScope __state,Exception __exception){Scope=__state;return __exception;}
}
[HarmonyPatch(typeof(FrameInfo),nameof(FrameInfo.Step))]
internal static class CombatAttackStepPatch
{
    private static void Prefix(ref FrameInfo __0,ref float __1)
    {
        try
        {
            var c=CombatActions.Current;var scope=CombatActorPatch.Scope;if(scope==null||c?.ModesBound!=true)return;
            if(scope.First)
            {
                scope.First=false;var actions=scope.Actor.activeActions;var plan=new List<(int,int,bool)>();
                for(int i=actions.Count-1;i>=0;i--){var a=actions[i];plan.Add((a.id,a.frameInfo.integerFrame,a.data!=null&&scope.Attacks.Contains(a.data.Pointer)));}
                scope.Plan=new(plan);scope.Plan.Next(__0.integerFrame);return;
            }
            if(scope.Plan.Next(__0.integerFrame)&&float.IsFinite(__1)&&__1>0)__1*=c.AttackMultiplier;
        }
        catch(Exception e){CombatActorPatch.Scope=null;CombatActions.HookError(e);}
    }
}
[HarmonyPatch(typeof(CheckCooldownFeature),nameof(CheckCooldownFeature.CanStart))]
internal static class CombatToolPatch
{
    private static void Postfix(CheckCooldownFeature __instance,Entity __0,ActionData __1,ref bool __result)
    {try{var c=CombatActions.Current;if(c?.ToolCooldown==true&&c.ModesBound&&c.IsPlayer(__0)&&__1!=null&&c.WeaponActions(true).Contains(__1.Pointer))__result=true;}catch(Exception e){CombatActions.HookError(e);}}
}
[HarmonyPatch(typeof(ConsumeEnergySlotActionFeature),nameof(ConsumeEnergySlotActionFeature.CanStart))]
internal static class CombatToolCostCheckPatch
{
    private static void Postfix(Entity __0,ActionData __1,ref bool __result)
    {try{if(CombatActions.Current?.CanBypassToolCost(__0,__1)==true)__result=true;}catch(Exception e){CombatActions.HookError(e);}}
}
[HarmonyPatch(typeof(ConsumeEnergySlotActionFeature),nameof(ConsumeEnergySlotActionFeature.Fire))]
internal static class CombatToolCostFirePatch
{
    // This native feature only calls EnergyComponent.ConsumeEnergySlot; other action features still execute.
    private static bool Prefix(Entity __0,ActionInstance __1)
    {try{return CombatActions.Current?.CanBypassToolCost(__0,__1?.data)!=true;}catch(Exception e){CombatActions.HookError(e);return true;}}
}
[HarmonyPatch(typeof(ApplicationFlowManager),nameof(ApplicationFlowManager.SendSignal))]
internal static class CombatPracticeExitPatch
{private static void Prefix(ApplicationFlowManager __instance,ApplicationSignal __0){try{CombatActions.Current?.NativeExit(__instance,__0);}catch(Exception e){Plugin.MenuLog.LogWarning("[RPCheat] practice_exit_observer "+e);}}}
