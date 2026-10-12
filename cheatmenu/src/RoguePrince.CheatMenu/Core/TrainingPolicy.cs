// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;

public readonly record struct TrainingScope(long Generation,long Player,int Scene,long Room,float RoomX,float RoomY)
{
    public bool Valid=>Generation>0 && Player!=0 && Room!=0 && float.IsFinite(RoomX) && float.IsFinite(RoomY);
    public bool Matches(TrainingScope other)=>Valid && other.Valid && this==other;
}
public static class TrainingValuePolicy
{
    public static float Speed(float value)
    {
        if(!float.IsFinite(value)||value<.25f||value>2f) throw new ArgumentException(MenuText.Get("速度范围为 0.25–2 倍"));
        return value;
    }
    public static float Zoom(float value)
    {
        if(!float.IsFinite(value)||value < -5 || value > 5) throw new ArgumentException(MenuText.Get("镜头缩放偏移范围为 -5–5"));
        return value;
    }
}
public sealed class ScopedScalarLease
{
    public bool Active {get;private set;}
    public float Original {get;private set;}
    public float Written {get;private set;}
    public void Capture(float before,float written)
    {
        if(!float.IsFinite(before)||!float.IsFinite(written)) throw new ArgumentException(MenuText.Get("原生数值无效"));
        if(!Active || Math.Abs(before-Written)>.00001f) Original=before;
        Written=written; Active=true;
    }
    public void Apply(float value,Func<float> read,Action<float> write)
    {
        var before=read();
        if(!float.IsFinite(before)||!float.IsFinite(value))throw new ArgumentException(MenuText.Get("原生数值无效"));
        var previousOwned=Active && Math.Abs(before-Written)<.00001f;
        var original=previousOwned?Original:before;
        try
        {
            write(value);var actual=read();
            if(!float.IsFinite(actual)||Math.Abs(actual-value)>.0001f)throw new InvalidOperationException(MenuText.Get("速度倍率回读失败"));
            Capture(before,value);
        }
        catch(Exception failure)
        {
            try
            {
                var actual=read();if(!float.IsFinite(actual)||Math.Abs(actual-before)>.0001f)write(before);
                actual=read();if(!float.IsFinite(actual)||Math.Abs(actual-before)>.0001f)throw new InvalidOperationException(MenuText.Get("速度恢复失败，请使用恢复正常速度"));
                if(!previousOwned)Clear();
            }
            catch(Exception restore)
            {
                Original=original;Written=value;Active=true;
                try{var current=read();if(float.IsFinite(current))Written=current;}catch{ /* Keep the request's recovery handle even when native reads fail. */ }
                throw new AggregateException(failure,restore);
            }
            throw;
        }
    }
    public float RestoreValue(float current)=>Active && Math.Abs(current-Written)<.00001f ? Original : current;
    public void Clear()=>Active=false;
}
public sealed class BatchSpawnQueue
{
    public bool Active {get;private set;}
    public bool Awaiting {get;private set;}
    public string MobId {get;private set;}
    public int Total {get;private set;}
    public int Completed {get;private set;}
    private double interval,nextAt;
    public void Start(string id,int count,double delay,double now)
    {
        var mob=MobCatalogPolicy.Find(id);
        if(Active || mob?.Supported!=true || mob.Boss || count<1 || count>10 || !double.IsFinite(delay) || delay<.6 || delay>10 || !double.IsFinite(now))
            throw new ArgumentException(MenuText.Get("批量仅支持普通怪物：数量 1–10，间隔 0.6–10 秒"));
        MobId=id;Total=count;Completed=0;interval=delay;nextAt=now;Awaiting=false;Active=true;
    }
    public bool Ready(double now)=>Active && !Awaiting && double.IsFinite(now) && now+1e-6>=nextAt;
    public void Issued() { if(!Active || Awaiting) throw new InvalidOperationException(MenuText.Get("批量请求状态无效")); Awaiting=true; }
    public void IssuedAt(double now) { if(!Ready(now)) throw new InvalidOperationException(MenuText.Get("批量请求尚未到时")); Issued(); }
    public void Complete(double now,bool success)
    {
        if(!Active || !Awaiting) throw new InvalidOperationException(MenuText.Get("批量没有待确认请求"));
        Awaiting=false;
        if(!success || !double.IsFinite(now)) {Active=false;return;}
        Completed++; if(Completed>=Total) Active=false; else nextAt=now+interval;
    }
    public void Cancel(){Active=false;Awaiting=false;}
    public bool Submitted(long previousId,long currentId,bool accepted,bool failed,double now)
    {
        if(accepted){IssuedAt(now);return true;}
        if(currentId!=previousId && failed)Cancel();
        return false;
    }
}
public sealed class RoomBookmark
{
    private TrainingScope scope;
    public bool HasPoint {get;private set;}
    public float X {get;private set;}
    public float Y {get;private set;}
    public float Z {get;private set;}
    public void Capture(TrainingScope current,float x,float y,float z)
    {
        if(!current.Valid || !float.IsFinite(x)||!float.IsFinite(y)||!float.IsFinite(z)) throw new ArgumentException(MenuText.Get("书签作用域或坐标无效"));
        scope=current;X=x;Y=y;Z=z;HasPoint=true;
    }
    public bool Matches(TrainingScope current)=>HasPoint && scope.Matches(current);
    public void Observe(TrainingScope current){if(HasPoint && !scope.Matches(current)) Clear();}
    public void Clear()=>HasPoint=false;
}
public static class StatusEffectView
{
    public static string Remaining(int frames,float frameDuration)
    {
        if(frames<0) return MenuText.Format("无倒计时（原生值 {0}）",frames);
        if(!float.IsFinite(frameDuration)||frameDuration<=0) return MenuText.Format("剩余 {0} 原生帧",frames);
        return MenuText.Format("剩余 {0:0.##} 秒 / {1} 帧",frames*(double)frameDuration,frames);
    }
}
