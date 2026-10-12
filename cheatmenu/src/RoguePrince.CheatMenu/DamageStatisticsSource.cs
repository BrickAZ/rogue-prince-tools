// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;

internal sealed class DamageSample
{
    internal DamageCredit Credit {get;} internal TrainingScope Scope {get;}
    internal long Context {get;} internal long Target {get;} internal long Epoch {get;} internal int Frame {get;}
    internal DamageSample(DamageCredit credit,TrainingScope scope,long context,long target,long epoch,int frame)
    {Credit=credit;Scope=scope;Context=context;Target=target;Epoch=epoch;Frame=frame;}
}
internal static class DamageStatisticsSource
{
    internal static DamageSample Capture(GameSession game,Entity target,DamageSystem.Connection connection,Entity fallback,long epoch)
    {
        if(!Application.isFocused||!game.Ready||target==null||target.destroyed||!target.gameObject.activeInHierarchy)return null;
        var scope=TrainingActions.ReadScope(game);
        var context=game.Player?.context?.Pointer.ToInt64()??0;
        if(!scope.Valid||context==0||target.context?.Pointer.ToInt64()!=context||target.gameObject.scene.handle!=scope.Scene)return null;
        var nativeSource=connection?.source;
        var actual=nativeSource??fallback;
        if(actual!=null&&(actual.destroyed||actual.context?.Pointer.ToInt64()!=context||actual.gameObject.scene.handle!=scope.Scene))return null;
        var caster=connection?.sourceCaster;
        var delivery=caster?.TryCast<DirectDamageCaster>()!=null?DamageDelivery.DirectCast:
            caster?.TryCast<DamageCaster>()!=null?
                connection.sourceSpawnedObject!=null?DamageDelivery.SpawnedCollision:DamageDelivery.Collision:DamageDelivery.Unknown;
        var item=connection?.sourceItem?.source;
        var credit=DamageCreditPolicy.Resolve(game.PlayerPointer.ToInt64(),nativeSource?.Pointer.ToInt64()??0,fallback?.Pointer.ToInt64()??0,
            target.Pointer==game.PlayerPointer,delivery,item==null?"":item.Guid.guid,item==null?"":ItemBrowser.ReadLabel(item.title,item.name).Display,target.name);
        var sample=new DamageSample(credit,scope,context,target.Pointer.ToInt64(),epoch,Time.frameCount);
        return IsCurrent(game,sample,target)?sample:null;
    }
    internal static bool IsCurrent(GameSession game,DamageSample sample,Entity target)
        =>sample!=null&&Application.isFocused&&game.Ready&&sample.Scope.Matches(TrainingActions.ReadScope(game))&&
            game.Player?.context?.Pointer.ToInt64()==sample.Context&&target!=null&&!target.destroyed&&target.Pointer.ToInt64()==sample.Target&&
            target.context?.Pointer.ToInt64()==sample.Context&&target.gameObject.scene.handle==sample.Scope.Scene;
}
