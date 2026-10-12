using System.Reflection;
using RoguePrince.CheatMenu.Core;

internal static class NextRescueTests
{
    private static Type Policy => typeof(MenuText).Assembly.GetType("RoguePrince.CheatMenu.Core.NextRescuePolicy")
        ?? throw new Exception("Missing production feature: NextRescuePolicy");
    private static object Call(object policy, string method, params object[] args)
    {
        try { return Policy.GetMethod(method).Invoke(policy, args); }
        catch (TargetInvocationException e) { throw e.InnerException; }
    }
    private static void Assert(bool value) { if (!value) throw new Exception("Unexpected rescue behavior"); }
    private static object Armed()
    {
        var p = Activator.CreateInstance(Policy);
        Assert((bool)Call(p,"TryArm",true,false,"run-1",42,"origin")); return p;
    }
    public static void Run(Action<string,Action> check)
    {
        check("unsupported next rescue map performs no native write and never carries to another map",()=>{
            var p=Armed();int writes=0;
            Assert((string)Call(p,"Begin","run-1",42,"boss",false,0f,(Action)(()=>writes++))=="unsupported");
            Assert((string)Call(p,"Begin","run-1",42,"supported",true,1f,(Action)(()=>writes++))=="idle");
            Assert(writes==0);
        });
        check("supported rescue generation writes once across retries",()=>{
            var p=Armed();int writes=0;
            Assert((string)Call(p,"Begin","run-1",42,"next",true,0f,(Action)(()=>writes++))=="prepared");
            Assert((string)Call(p,"Begin","run-1",42,"next",true,1f,(Action)(()=>writes++))=="prepared");
            Assert(writes==1);
        });
        check("unarmed rescue generation leaves native scheduling unchanged",()=>{
            var p=Activator.CreateInstance(Policy);int writes=0;
            Assert((string)Call(p,"Begin","run-1",42,"next",true,0f,(Action)(()=>writes++))=="idle");Assert(writes==0);
        });
        check("next map without a biome generator cancels rescue without a deferred write",()=>{
            var p=Armed();int writes=0;
            Assert((string)Call(p,"Observe","run-1",42,true,false,"unsupported-world",1f)=="unsupported");
            Assert((string)Call(p,"Begin","run-1",42,"later",true,2f,(Action)(()=>writes++))=="idle");Assert(writes==0);
        });
        check("rescue cannot arm for dead player loading or missing run and origin",()=>{
            foreach(var row in new[]{(false,false,"run-1","origin"),(true,true,"run-1","origin"),(true,false,"","origin"),(true,false,"run-1","")}) {
                var p=Activator.CreateInstance(Policy);Assert(!(bool)Call(p,"TryArm",row.Item1,row.Item2,row.Item3,42,row.Item4));
            }
        });
        check("second rescue click cannot overwrite an existing reservation",()=>{
            var p=Armed();Assert(!(bool)Call(p,"TryArm",true,false,"other-run",99,"other-origin"));
        });
        check("current map regeneration is not the next rescue map",()=>{
            var p=Armed();int writes=0;
            Assert((string)Call(p,"Begin","run-1",42,"origin",true,0f,(Action)(()=>writes++))=="current");
            Assert((string)Call(p,"Begin","run-1",42,"next",true,1f,(Action)(()=>writes++))=="prepared");Assert(writes==1);
        });
        foreach(var row in new[]{("run-1",43),("run-2",42)})
            check("new run cannot inherit rescue reservation "+row,()=>{
                var p=Armed();int writes=0;
                Assert((string)Call(p,"Begin",row.Item1,row.Item2,"next",true,0f,(Action)(()=>writes++))=="context_changed");
                Assert((string)Call(p,"Begin","run-1",42,"next",true,1f,(Action)(()=>writes++))=="idle");Assert(writes==0);
            });
        check("cancelled rescue never modifies later generation",()=>{
            var p=Armed();Call(p,"Cancel");int writes=0;
            Assert((string)Call(p,"Begin","run-1",42,"next",true,0f,(Action)(()=>writes++))=="idle");Assert(writes==0);
        });
        check("failed native rescue write clears reservation and cannot auto retry",()=>{
            var p=Armed();int writes=0;
            try {Call(p,"Begin","run-1",42,"next",true,0f,(Action)(()=>{writes++;throw new IOException("native failure");}));throw new Exception("failure hidden");}catch(IOException){}
            Assert((string)Call(p,"Begin","run-1",42,"next",true,1f,(Action)(()=>writes++))=="idle");Assert(writes==1);
        });
        check("rescue arrival cannot consume reservation before native parameters confirm",()=>{
            var p=Armed();Call(p,"Begin","run-1",42,"next",true,0f,(Action)(()=>{}));
            Assert((string)Call(p,"Observe","run-1",42,true,false,"next",1f)=="pending");
            Assert((bool)Call(p,"Confirm","next",true,(Action)(()=>{})));
            Assert((string)Call(p,"Observe","run-1",42,true,true,"next",2f)=="pending");
            Assert((string)Call(p,"Observe","run-1",42,true,false,"next",3f)=="completed");
            Assert((string)Call(p,"Begin","run-1",42,"later",true,4f,(Action)(()=>throw new Exception("carried")))=="idle");
        });
        check("wrong native rescue parameters fail without reporting generation success",()=>{
            var p=Armed();Call(p,"Begin","run-1",42,"next",true,0f,(Action)(()=>{}));
            Assert(!(bool)Call(p,"Confirm","next",false,(Action)(()=>throw new Exception("invalid parameters restored"))));
            Assert((string)Call(p,"Observe","run-1",42,true,false,"next",1f)=="idle");
        });
        check("rescue timeout never carries or repeats the native write",()=>{
            var p=Armed();int writes=0;Call(p,"Begin","run-1",42,"next",true,0f,(Action)(()=>writes++));
            Assert((string)Call(p,"Observe","run-1",42,true,true,"next",61f)=="failed");
            Assert((string)Call(p,"Begin","run-1",42,"later",true,62f,(Action)(()=>writes++))=="idle");Assert(writes==1);
        });
        check("native parameter confirmation restores the temporary schedule before consuming the request",()=>{
            var p=Armed();int schedule=5,restores=0;
            Call(p,"Begin","run-1",42,"next",true,0f,(Action)(()=>schedule=3));
            Assert((bool)Call(p,"Confirm","next",true,(Action)(()=>{schedule=5;restores++;})));
            Assert(schedule==5&&restores==1);
            Assert((string)Call(p,"Observe","run-1",42,true,false,"next",1f)=="completed");
        });
        check("failed post-selection restoration cannot report successful rescue confirmation",()=>{
            var p=Armed();Call(p,"Begin","run-1",42,"next",true,0f,(Action)(()=>{}));
            try {Call(p,"Confirm","next",true,(Action)(()=>throw new IOException("restore failure")));throw new Exception("hidden restore failure");}catch(IOException){}
            Assert((string)Call(p,"Observe","run-1",42,true,false,"next",1f)=="idle");
        });
        check("repeated native start does not restore an already released rescue schedule twice",()=>{
            var p=Armed();Call(p,"Begin","run-1",42,"next",true,0f,(Action)(()=>{}));
            Assert((bool)Call(p,"Confirm","next",true,(Action)(()=>{})));
            Assert((bool)Call(p,"Confirm","next",true,(Action)(()=>throw new Exception("restored twice"))));
        });
        check("ending a run clears its rescue reservation",()=>{
            var p=Armed();Assert((string)Call(p,"Observe","run-1",42,false,false,"origin",1f)=="context_changed");
        });
        check("rescue transaction restores native schedule after failed readback",()=>{
            var type=typeof(MenuText).Assembly.GetType("RoguePrince.CheatMenu.Core.RescueWriteTransaction")??throw new Exception("Missing production feature: RescueWriteTransaction");
            int layer=5;
            try { type.GetMethod("Apply").Invoke(null,new object[]{(Action)(()=>layer=3),(Action)(()=>throw new IOException("readback")),(Action)(()=>layer=5)}); }
            catch(TargetInvocationException e) {Assert(e.InnerException is InvalidOperationException);}
            Assert(layer==5);
        });
    }
}
