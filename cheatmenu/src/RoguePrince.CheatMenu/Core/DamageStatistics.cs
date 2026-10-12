// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;

public enum DamageCreditKind { Player, OtherSource, UnknownSource, SelfDamage }
public enum DamageDelivery { Collision, SpawnedCollision, DirectCast, Unknown }
public readonly record struct DamageCredit(DamageCreditKind Kind,DamageDelivery Delivery,long Source,string ItemId,string ItemName,string TargetName);
public static class DamageCreditPolicy
{
    public static DamageCredit Resolve(long player,long nativeSource,long fallbackSource,bool targetIsPlayer,DamageDelivery delivery,string itemId,string itemName,string targetName)
    {
        // A non-null native source takes precedence. Never OR conflicting owners together.
        var source=nativeSource!=0?nativeSource:fallbackSource;
        var kind=player==0?DamageCreditKind.UnknownSource:targetIsPlayer?DamageCreditKind.SelfDamage:
            source==0?DamageCreditKind.UnknownSource:source==player?DamageCreditKind.Player:DamageCreditKind.OtherSource;
        return new(kind,delivery,source,Text(itemId),Text(itemName),Text(targetName));
    }
    private static string Text(string value)
    {
        if(string.IsNullOrEmpty(value))return "";
        return new string(value.Take(128).Select(c=>char.IsControl(c)?' ':c).ToArray());
    }
}
public sealed class DamageGroup
{
    public string ItemId {get;init;} public string ItemName {get;init;} public DamageDelivery Delivery {get;init;}
    public double Damage {get;internal set;} public long Hits {get;internal set;} public bool Overflow {get;init;}
}
public readonly record struct DamageHit(long Sequence,long Generation,int Frame,long Target,double Damage,DamageCredit Credit);
public sealed class DamageStatistics
{
    public const int RecentLimit=64,GroupLimit=32;
    public long Epoch {get;private set;}
    public long OtherHits {get;private set;} public long UnknownHits {get;private set;} public long SelfHits {get;private set;}
    private readonly List<DamageGroup> groups=new();
    private readonly List<DamageHit> recent=new();
    private long sequence;
    public IReadOnlyList<DamageGroup> Groups=>groups;
    public IReadOnlyList<DamageHit> Recent=>recent;
    public bool Record(long target,double loss,double nested,DamageCredit credit,long generation,int frame)
    {
        var amount=loss-nested;
        if(target==0||generation<=0||frame<0||!double.IsFinite(loss)||!double.IsFinite(nested)||nested<0||!double.IsFinite(amount)||amount<=0)return false;
        switch(credit.Kind)
        {
            case DamageCreditKind.UnknownSource:UnknownHits++;return false;
            case DamageCreditKind.OtherSource:OtherHits++;return false;
            case DamageCreditKind.SelfDamage:SelfHits++;return false;
            case DamageCreditKind.Player:break;
            default:return false;
        }
        var group=groups.FirstOrDefault(g=>!g.Overflow&&g.ItemId==credit.ItemId&&g.Delivery==credit.Delivery);
        if(group==null)
        {
            if(groups.Count<GroupLimit-1)
            {
                group=new(){ItemId=credit.ItemId,ItemName=credit.ItemName,Delivery=credit.Delivery};groups.Add(group);
            }
            else
            {
                group=groups.FirstOrDefault(g=>g.Overflow);
                if(group==null){group=new(){ItemId="",ItemName="",Delivery=DamageDelivery.Unknown,Overflow=true};groups.Add(group);}
            }
        }
        group.Damage+=amount;group.Hits++;
        if(recent.Count==RecentLimit)recent.RemoveAt(0);
        recent.Add(new(++sequence,generation,frame,target,amount,credit));
        return true;
    }
    public void Clear(){groups.Clear();recent.Clear();OtherHits=UnknownHits=SelfHits=sequence=0;Epoch++;}
}
