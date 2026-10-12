using System.Reflection;
using RoguePrince.CheatMenu.Core;
internal static class TrainingTests
{
    private static Type T(string name)=>typeof(MenuText).Assembly.GetType("RoguePrince.CheatMenu.Core."+name)??throw new Exception("Missing production training feature: "+name);
    private static dynamic New(string name,params object[] args)=>Activator.CreateInstance(T(name),args);
    private static object Call(string type,string name,params object[] args)=>T(type).GetMethod(name).Invoke(null,args);
    private static void Assert(bool ok){if(!ok)throw new Exception("Unexpected training behavior");}
    private static void Reject(Action a){try{a();}catch(Exception){return;}throw new Exception("Invalid training input accepted");}
    private static object Scope(long gen=1,long player=10,int scene=2,long room=20,float x=3,float y=4)=>New("TrainingScope",gen,player,scene,room,x,y);
    public static void Run(Action<string,Action> check)
    {
        check("training restore failure still attempts original player cleanup",()=>{
            var visited=new List<int>();var run=T("RecoverySteps").GetMethod("Run");
            Reject(()=>run.Invoke(null,new object[]{new Action[]{()=>{visited.Add(1);throw new Exception("training");},()=>visited.Add(2),()=>visited.Add(3)}}));
            Assert(visited.SequenceEqual(new[]{1,2,3}));
        });
        check("multiple cleanup failures are retained after all modules run",()=>{
            var run=T("RecoverySteps").GetMethod("Run");bool last=false;
            try{run.Invoke(null,new object[]{new Action[]{()=>throw new Exception("first"),()=>throw new Exception("second"),()=>last=true}});throw new Exception("cleanup accepted");}
            catch(TargetInvocationException e){Assert(last && e.InnerException is AggregateException a && a.InnerExceptions.Count==2);}
        });
        check("failed speed update retains earlier lease",()=>{
            dynamic l=New("ScopedScalarLease");float native=.5f;l.Capture(1f,.5f);bool attempted=false;
            Reject(()=>l.Apply(2f,(Func<float>)(()=>native),(Action<float>)(v=>{attempted=true;if(v==2)throw new Exception("write failed");native=v;})));
            Assert(attempted && l.Active && l.RestoreValue(native)==1f);
        });
        check("speed readback failure rolls back before preserving ownership",()=>{
            dynamic l=New("ScopedScalarLease");float native=1;
            Reject(()=>l.Apply(.5f,(Func<float>)(()=>native),(Action<float>)(v=>native=v==.5f?.7f:v)));
            Assert(native==1 && !l.Active);
        });
        check("speed unreadable after write keeps recovery owner",()=>{
            dynamic l=New("ScopedScalarLease");float native=1;int reads=0;
            Reject(()=>l.Apply(.5f,(Func<float>)(()=>++reads==1?native:throw new Exception("unreadable")),(Action<float>)(v=>native=v)));
            Assert(native==.5f && l.Active && l.Original==1f && l.Written==.5f);
        });
        check("new speed request adopts external baseline",()=>{
            dynamic l=New("ScopedScalarLease");l.Capture(1f,.5f);float native=.8f;
            l.Apply(2f,(Func<float>)(()=>native),(Action<float>)(v=>native=v));Assert(l.RestoreValue(native)==.8f);
        });
        check("native zoom needs consumer update before confirmation",()=>{
            dynamic l=New("DeferredNativeLease");l.Issue(10L);Assert(!l.Observe(10L,false,false) && l.Owned && l.Pending);
            Assert(l.Observe(11L,true,true) && !l.Pending && l.Owned);l.Release(11L);
            Assert(!l.Observe(11L,false,false) && l.Owned);Assert(!l.Observe(12L,true,true) && l.Owned);
            Assert(l.Observe(13L,false,false) && !l.Owned);
        });
        check("missing native zoom confirmation retains release handle",()=>{
            dynamic l=New("DeferredNativeLease");l.Issue(0L);Reject(()=>l.Observe(1L,false,false));Assert(l.Owned);l.Release(1L);Assert(l.Observe(2L,false,false) && !l.Owned);
        });
        check("accepted batch request failing asset load stops rather than retries",()=>{
            dynamic q=New("BatchSpawnQueue");q.Start("SwordMan",2,1d,0d);
            Assert(!q.Submitted(1L,2L,false,true,0d) && !q.Active && !q.Ready(10d));
        });
        check("batch cooldown rejection remains ready without treating stale error as new failure",()=>{
            dynamic q=New("BatchSpawnQueue");q.Start("SwordMan",2,1d,0d);
            Assert(!q.Submitted(1L,1L,false,true,0d) && q.Active && q.Ready(1d));
            Assert(q.Submitted(1L,2L,true,false,1d) && q.Awaiting && !q.Ready(5d));
        });
        check("zoom rejects nonfinite and out of range offsets",()=>{
            foreach(var v in new[]{float.NaN,float.PositiveInfinity,-6f,6f})Reject(()=>Call("TrainingValuePolicy","Zoom",v));
            foreach(var v in new[]{-5f,0f,5f})Assert((float)Call("TrainingValuePolicy","Zoom",v)==v);
        });
        check("batch waits for each native spawn confirmation and interval",()=>{
            dynamic q=New("BatchSpawnQueue");q.Start("SwordMan",3,.6d,0d);Assert(q.Ready(0d));q.Issued();Assert(!q.Ready(10d));q.Complete(1d,true);
            Assert(q.Completed==1 && !q.Ready(1.5d) && q.Ready(1.6d));q.Issued();q.Complete(2d,true);q.IssuedAt(2.6d);q.Complete(3d,true);Assert(!q.Active&&q.Completed==3);
        });
        foreach(var row in new[]{("SwordMan",0,.6d),("SwordMan",11,.6d),("SwordMan",1,.5d),("SwordMan",1,11d),("SwordMan",1,double.NaN),("Nogai",1,1d),("missing",1,1d)})
            check("invalid or boss batch rejects "+row,()=>{dynamic q=New("BatchSpawnQueue");Reject(()=>q.Start(row.Item1,row.Item2,row.Item3,0d));Assert(!q.Active);});
        check("batch stops on first failure and keeps confirmed count",()=>{
            dynamic q=New("BatchSpawnQueue");q.Start("SwordMan",4,1d,0d);q.Issued();q.Complete(1d,true);q.IssuedAt(2d);q.Complete(3d,false);Assert(!q.Active&&q.Completed==1&&!q.Ready(20d));
        });
        check("cancel prevents any later spawn",()=>{dynamic q=New("BatchSpawnQueue");q.Start("SwordMan",2,1d,0d);q.Cancel();Assert(!q.Ready(20d));});
        check("scope rejects changed player generation scene template and placed room",()=>{
            dynamic first=Scope();Assert(first.Matches((dynamic)Scope()));
            foreach(var changed in new[]{Scope(gen:2),Scope(player:11),Scope(scene:3),Scope(room:21),Scope(x:5),Scope(y:6)})Assert(!first.Matches((dynamic)changed));
        });
        check("bookmark belongs to one exact room and clears when leaving",()=>{
            dynamic b=New("RoomBookmark");b.Capture((dynamic)Scope(),1f,2f,3f);Assert(b.HasPoint&&b.Matches((dynamic)Scope()));
            b.Observe((dynamic)Scope(x:5));Assert(!b.HasPoint);Assert(!b.Matches((dynamic)Scope()));
        });
        check("nonfinite bookmark coordinate never replaces valid point",()=>{
            dynamic b=New("RoomBookmark");b.Capture((dynamic)Scope(),1f,2f,3f);Reject(()=>b.Capture((dynamic)Scope(),float.NaN,4f,5f));Assert(b.X==1f);
        });
        check("scalar lease restores original after repeated menu adjustments",()=>{
            dynamic l=New("ScopedScalarLease");l.Capture(1f,.5f);l.Capture(.5f,2f);Assert(l.RestoreValue(2f)==1f);l.Clear();Assert(!l.Active);
        });
        check("external scale writes are preserved",()=>{
            dynamic l=New("ScopedScalarLease");l.Capture(.8f,.5f);Assert(l.RestoreValue(.3f)==.3f && l.Active);
        });
        check("invalid speed never captures a lease",()=>{
            foreach(var speed in new[]{0f,.1f,3f,float.NaN,float.PositiveInfinity}){T("TrainingValuePolicy");Reject(()=>Call("TrainingValuePolicy","Speed",speed));}
        });
        check("valid slowdown and acceleration survive validation",()=>{foreach(var speed in new[]{.25f,.5f,1f,1.5f,2f})Assert((float)Call("TrainingValuePolicy","Speed",speed)==speed);});
        check("remaining effect seconds use native frame duration",()=>{
            MenuText.SetLanguage("zh-CN");var text=(string)Call("StatusEffectView","Remaining",120,.02f);Assert(text.Contains("2.4")&&text.Contains("120"));
        });
        check("negative effect duration is not interpreted as seconds",()=>{MenuText.SetLanguage("zh-CN");Assert(((string)Call("StatusEffectView","Remaining",-1,.02f)).Contains("无倒计时"));});
        check("unknown frame duration still shows exact native frames",()=>{MenuText.SetLanguage("zh-CN");Assert(((string)Call("StatusEffectView","Remaining",60,float.NaN)).Contains("60"));});
    }
}
