using System.Reflection;
using RoguePrince.CheatMenu.Core;

internal static class LevelAffixTests
{
    private static Type Required(string name) => typeof(MenuText).Assembly.GetType("RoguePrince.CheatMenu.Core." + name)
        ?? throw new Exception("Missing production feature: " + name);
    private static object Call(Type type, object instance, string method, params object[] args)
    {
        try { return type.GetMethod(method).Invoke(instance, args); }
        catch (TargetInvocationException e) { throw e.InnerException; }
    }
    private static void Assert(bool condition) { if (!condition) throw new Exception("Unexpected feature result"); }
    public static void Run(Action<string, Action> check)
    {
        check("pending affix recovery retires on player change without replaying to new player",()=>{
            var t=Required("ScopedAffixRecovery");var recovery=Activator.CreateInstance(t);int attempts=0;
            Call(t,recovery,"Set",1L,(Action)(()=>attempts++),"owner-1 original=a,b");
            Assert((bool)t.GetProperty("Pending").GetValue(recovery));
            Assert((string)Call(t,recovery,"Retire")=="owner-1 original=a,b");
            Assert(!(bool)t.GetProperty("Pending").GetValue(recovery));
            Assert(!(bool)Call(t,recovery,"Recover",2L));Assert(attempts==0);
        });
        check("affix recovery rejects a new generation and retains old owner record",()=>{
            var t=Required("ScopedAffixRecovery");var recovery=Activator.CreateInstance(t);int attempts=0;
            Call(t,recovery,"Set",1L,(Action)(()=>attempts++),"old-owner");
            try {Call(t,recovery,"Recover",2L);throw new Exception("new player accepted");}catch(InvalidOperationException){}
            Assert(attempts==0&&(bool)t.GetProperty("Pending").GetValue(recovery));
            Assert((bool)Call(t,recovery,"Recover",1L));Assert(attempts==1);
        });
        check("failed same-owner affix recovery remains retryable",()=>{
            var t=Required("ScopedAffixRecovery");var recovery=Activator.CreateInstance(t);int attempts=0;
            Call(t,recovery,"Set",1L,(Action)(()=>{if(++attempts==1)throw new IOException("still blocked");}),"owner");
            try {Call(t,recovery,"Recover",1L);}catch(IOException){}
            Assert((bool)t.GetProperty("Pending").GetValue(recovery));
            Assert((bool)Call(t,recovery,"Recover",1L));Assert(attempts==2);
        });
        foreach(var row in new[]{(1L,10L,20L,30L,3,true),(2L,10L,20L,30L,3,false),(1L,11L,20L,30L,3,false),
            (1L,10L,21L,30L,3,false),(1L,10L,20L,31L,3,false),(1L,10L,20L,30L,4,false),(1L,0L,20L,30L,3,false)})
            check("affix restoration scope " + row,()=>Assert((bool)Call(Required("AffixOwnerScope"),null,"IsCurrent",1L,row.Item1,(IntPtr)10,(IntPtr)row.Item2,(IntPtr)20,(IntPtr)row.Item3,(IntPtr)30,(IntPtr)row.Item4,3,row.Item5)==row.Item6));
        check("affix ownership keeps identical affixes on the other weapon", () => {
            var type=typeof(MenuText).Assembly.GetType("RoguePrince.CheatMenu.WeaponAffixOwnership") ?? throw new Exception("Missing production ownership predicate");
            var first=new ItemInstanceData {Pointer=(IntPtr)100};var other=new ItemInstanceData {Pointer=(IntPtr)200};
            var reread=new ItemInstanceData {Pointer=(IntPtr)100};
            var own=new ItemInstanceData {Pointer=(IntPtr)300,Affix=new(){targetItem=reread}};
            var foreign=new ItemInstanceData {Pointer=(IntPtr)400,Affix=new(){targetItem=other}};
            Assert((bool)Call(type,null,"IsOwnedBy",first,own));
            Assert(!(bool)Call(type,null,"IsOwnedBy",first,foreign));
            Assert(!(bool)Call(type,null,"IsOwnedBy",first,first));
            Assert(!(bool)Call(type,null,"IsOwnedBy",first,new ItemInstanceData {Affix=new()}));
            Assert(!(bool)Call(type,null,"IsOwnedBy",null,own));
        });
        foreach (var row in new[] {
            (1,1,true,false,false,false,true), (2,1,true,false,false,false,false),
            (7,2,true,false,false,false,true), (1,1,false,false,false,false,false),
            (1,1,true,true,false,false,false), (1,1,true,false,true,false,false),
            (1,1,true,false,false,true,false), (7,0,true,false,false,false,false) })
            check("affix eligibility " + row, () => Assert((bool)Call(Required("AffixSelectionPolicy"),null,"CanAdd",row.Item1,row.Item2,row.Item3,row.Item4,row.Item5,row.Item6)==row.Item7));
        check("affix failed replacement restores original object state", () => {
            object original=new(); object current=original;
            try { Call(Required("AffixEditTransaction"),null,"Apply",(Action)(()=>{current=new();throw new IOException("partial add");}),(Action)(()=>{}),(Action)(()=>current=original)); }
            catch(InvalidOperationException) { }
            Assert(ReferenceEquals(current,original));
        });
        check("affix failed readback rolls back partial native success", () => {
            string[] original={"a","b"}; string[] current=original;
            try { Call(Required("AffixEditTransaction"),null,"Apply",(Action)(()=>current=new[]{"c"}),(Action)(()=>throw new IOException("readback")),(Action)(()=>current=original)); }
            catch(InvalidOperationException) { }
            Assert(ReferenceEquals(current,original));
        });
        check("affix successful replacement is applied exactly once", () => {
            int writes=0,restores=0;
            Call(Required("AffixEditTransaction"),null,"Apply",(Action)(()=>writes++),(Action)(()=>Assert(writes==1)),(Action)(()=>restores++));
            Assert(writes==1&&restores==0);
        });
        check("affix failed rollback exposes both failures", () => {
            try { Call(Required("AffixEditTransaction"),null,"Apply",(Action)(()=>throw new IOException("apply")),(Action)(()=>{}),(Action)(()=>throw new IOException("restore"))); }
            catch(AggregateException e) { Assert(e.InnerExceptions.Count==2); return; }
            throw new Exception("rollback failure hidden");
        });
        check("level rejects a dead player, loading, missing or same target", () => {
            var t=Required("LevelChangeGate");var gate=Activator.CreateInstance(t);
            foreach(var row in new[]{(false,false,true,"a","b"),(true,true,true,"a","b"),(true,false,false,"a","b"),(true,false,true,"a","a")})
                Assert(!(bool)Call(t,gate,"TryBegin",row.Item1,row.Item2,row.Item3,row.Item4,row.Item5,0f));
        });
        check("level blocks repeat clicks and completes only on arrival", () => {
            var t=Required("LevelChangeGate");var gate=Activator.CreateInstance(t);
            Assert((bool)Call(t,gate,"TryBegin",true,false,true,"a","b",0f));
            Assert(!(bool)Call(t,gate,"TryBegin",true,false,true,"a","c",1f));
            Assert((string)Call(t,gate,"Observe",true,"b",2f)=="pending");
            Assert((string)Call(t,gate,"Observe",false,"b",3f)=="completed");
            Assert((bool)Call(t,gate,"TryBegin",true,false,true,"b","c",4f));
        });
        check("level stalled load reports failure without resending", () => {
            var t=Required("LevelChangeGate");var gate=Activator.CreateInstance(t);
            Assert((bool)Call(t,gate,"TryBegin",true,false,true,"a","b",0f));
            Assert((string)Call(t,gate,"Observe",false,"a",31f)=="failed");
            Assert(!(bool)t.GetProperty("Busy").GetValue(gate));
        });
    }
}
