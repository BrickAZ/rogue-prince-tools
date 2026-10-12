using System.Numerics;
using RoguePrince.CheatMenu.Core;
int passed=0,failed=0;
void Test(string name,Action test){try{test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
void Check(bool condition){if(!condition)throw new Exception("assertion failed");}
ReceiverShape Shape(ReceiverShapeKind kind)=>new(kind,new(5,6,7),Quaternion.Identity,new(1,2,3),1,6,1);
Test("box has 12 edges with exact world extents",()=>{var lines=ReceiverWireframe.Build(Shape(ReceiverShapeKind.Box));Check(lines.Count==12);var points=lines.SelectMany(l=>new[]{l.First,l.Second}).ToArray();Check(points.Min(p=>p.X)==4&&points.Max(p=>p.Y)==8&&points.Min(p=>p.Z)==4);});
Test("sphere has three great circles",()=>{var s=Shape(ReceiverShapeKind.Sphere);var lines=ReceiverWireframe.Build(s);Check(lines.Count==72);Check(lines.All(l=>Math.Abs(Vector3.Distance(l.First,s.Center)-1)<.0001f));Check(lines.Any(l=>l.First.Z>s.Center.Z+.9f));});
foreach(var axis in new[]{0,1,2}){var a=axis;Test("capsule rounded caps and axial height "+a,()=>{var s=Shape(ReceiverShapeKind.Capsule) with {Axis=a};var lines=ReceiverWireframe.Build(s);Check(lines.Count==100);var points=lines.SelectMany(l=>new[]{l.First-s.Center,l.Second-s.Center}).ToArray();float Value(Vector3 p)=>a==0?p.X:a==1?p.Y:p.Z;Check(Math.Abs(points.Max(Value)-3)<.0001f&&Math.Abs(points.Min(Value)+3)<.0001f);});}
Test("short capsule clamps to sphere",()=>Check(ReceiverWireframe.Build(Shape(ReceiverShapeKind.Capsule) with {Height=.5f}).Count==72));
Test("rotated box preserves orientation",()=>{var lines=ReceiverWireframe.Build(Shape(ReceiverShapeKind.Box) with {Rotation=Quaternion.CreateFromAxisAngle(Vector3.UnitZ,MathF.PI/2)});Check(lines.Count==12);Check(Math.Abs(lines.SelectMany(l=>new[]{l.First.X,l.Second.X}).Max()-7)<.0001f);});
foreach(var value in new[]{float.NaN,float.PositiveInfinity,-1f,101f}){var v=value;Test("invalid radius rejected "+v,()=>Check(ReceiverWireframe.Build(Shape(ReceiverShapeKind.Sphere) with {Radius=v}).Count==0));}
Test("nonfinite position rejected",()=>Check(ReceiverWireframe.Build(Shape(ReceiverShapeKind.Box) with {Center=new(float.NaN,0,0)}).Count==0));
Test("invalid capsule axis rejected",()=>Check(ReceiverWireframe.Build(Shape(ReceiverShapeKind.Capsule) with {Axis=3}).Count==0));
Test("zero quaternion rejected",()=>Check(ReceiverWireframe.Build(Shape(ReceiverShapeKind.Box) with {Rotation=default}).Count==0));
Test("negative box extent rejected",()=>Check(ReceiverWireframe.Build(Shape(ReceiverShapeKind.Box) with {Extents=new(-1,1,1)}).Count==0));
AdapterTests.Run(Test);
Console.WriteLine($"RESULT {passed} passed, {failed} failed");
return failed==0?0:1;
