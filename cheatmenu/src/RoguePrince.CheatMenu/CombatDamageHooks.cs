// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.EntityFramework;
namespace RoguePrince.CheatMenu;
internal static class DamageHooks
{
    internal sealed class Operation
    {internal Damageable Health;internal IntPtr Target;internal float Before;internal double Nested;internal bool Outgoing,Player,CannotKill;internal long Generation;internal DamageSample Sample;}
    [ThreadStatic] internal static Stack<Operation> Stack;
    internal static void Begin(Damageable health,DamageSystem.Connection connection,Entity source,out Operation operation)
    {
        operation=null;var c=CombatActions.Current;if(c==null||!c.Bound||health==null||!c.Active)return;
        var entity=health._entity;if(entity==null)return;
        operation=new(){Health=health,Target=entity.Pointer,Before=health.currentHealth,Generation=c.Generation,Player=c.IsPlayer(entity),Outgoing=c.IsPlayer(connection?.source)||c.IsPlayer(source),CannotKill=connection?.damageData.canNotKillTarget==true};
        (Stack??=new()).Push(operation);
        if(c.Recording)
        {
            try{operation.Sample=DamageStatisticsSource.Capture(c.Game,entity,connection,source,c.Ledger.Details.Epoch);}
            catch(Exception e){StatisticsError(c,e);}
        }
    }
    internal static void End(Operation op,Exception exception)
    {
        if(op==null)return;
        if(Stack?.Count>0&&ReferenceEquals(Stack.Peek(),op))Stack.Pop();else{Stack?.Clear();return;}
        if(exception!=null)return;
        var loss=Math.Max(0,op.Before-Math.Max(0,op.Health.currentHealth));
        var sameTargetParent=false;
        foreach(var parent in Stack)if(parent.Target==op.Target){parent.Nested+=loss;sameTargetParent=true;break;}
        var c=CombatActions.Current;if(c==null||!c.Bound||c.Generation!=op.Generation)return;
        if(c.Recording&&op.Sample!=null)
        {
            try
            {
                if(op.Sample.Epoch==c.Ledger.Details.Epoch&&DamageStatisticsSource.IsCurrent(c.Game,op.Sample,op.Health._entity))
                    c.Ledger.RecordDetailed(op.Target.ToInt64(),loss,op.Nested,op.Sample.Credit,op.Generation,op.Sample.Frame);
            }
            catch(Exception e){StatisticsError(c,e);}
        }
        if(op.Player&&loss-op.Nested>0)c.HitPlayer();
        // Refill after measuring the loss, so a dummy still reports damage.
        if(!sameTargetParent&&c.Dummy==op.Target&&op.Health.isAlive&&!op.Health.onDieActionStarted)op.Health.currentHealth=op.Health.maxHealth;
    }
    private static void StatisticsError(CombatActions c,Exception e)
    {c.Recording=false;Plugin.MenuLog.LogWarning("[RPCheat] damage_statistics_disabled "+e);}
}
