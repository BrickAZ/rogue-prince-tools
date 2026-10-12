// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes;
using MotherBase.ActionFramework;
using UnityEngine;
using UObject=UnityEngine.Object;
namespace RoguePrince.CheatMenu;

// Clone only combat assets. Animation, feedback, stats and addressable references
// are read-only shared inputs. Native serialized classes are explicitly copied;
// no list or action feature on a loaded game asset is ever rewritten in place.
internal sealed class BossSpawnAssetGraph : IDisposable
{
 private readonly Dictionary<IntPtr,object> copies=new();
 private readonly List<UObject> assets=new();
 private readonly Func<Type,ScriptableObject> roomVariable;
 private readonly EntityVariable bossEntityVariable;
 internal int Count=>assets.Count;
 internal BossSpawnAssetGraph(Func<Type,ScriptableObject> roomVariable,EntityVariable bossEntityVariable=null)
 {this.roomVariable=roomVariable;this.bossEntityVariable=bossEntityVariable;}
 private static object Concrete(Il2CppObjectBase value)
 {
  var name=value.Cast<Il2CppSystem.Object>().GetIl2CppType().FullName.Replace('/','+');
  var type=typeof(AltanBossBrain).Assembly.GetType(name)??typeof(ActionData).Assembly.GetType(name);
  return type==null?value:Activator.CreateInstance(type,value.Pointer);
 }
 private static bool SerializedType(Type t)=>typeof(Il2CppObjectBase).IsAssignableFrom(t)&&!typeof(UObject).IsAssignableFrom(t)&&
  (t.Assembly==typeof(AltanBossBrain).Assembly||t.Assembly==typeof(ActionData).Assembly)&&
  t.Namespace?.StartsWith("Il2Cpp",StringComparison.Ordinal)!=true&&
  !typeof(UnityEngine.AddressableAssets.AssetReference).IsAssignableFrom(t);
 private static bool Plain(Type t)=>SerializedType(t)&&!t.IsAbstract&&t.GetConstructor(Type.EmptyTypes)!=null;
 internal void Bind(ScriptableObject source,ScriptableObject replacement){if(source!=null)copies[source.Pointer]=replacement;}
 private bool CombatAsset(object value)=>value is ActionTrigger||value is ActionFeature||value is ActionBindings||value is ActorConfig||
  value is AltanBossBrain||value is BaatarBossBrain||value is GoatBossBrain||value is CorruptedNogaiBrain||value is KaaliBossBrain||value is NogaiBossBrain||
  value is EffectScriptableObject||value is BackWallConfiguration||value?.GetType()==typeof(SpawnBackWallConfiguration);
 internal T Clone<T>(T source) where T:UObject=>(T)Rewrite(source);
 private object Rewrite(object value)
 {
  if(value is not Il2CppObjectBase native)return value;
  if(copies.TryGetValue(native.Pointer,out var found))return found;
  if(copies.Count>=4096)throw new InvalidOperationException("Boss combat graph exceeded its node bound");
  value=Concrete(native);var type=value.GetType();
  if(value is ScriptableObject script)
  {
   if(script is EntityVariable&&script.name=="BossEntityVariable"&&bossEntityVariable!=null)
   {copies.Add(native.Pointer,bossEntityVariable);return bossEntityVariable;}
   var variable=roomVariable(type);
   if(variable!=null){copies.Add(native.Pointer,variable);return variable;}
   if(!CombatAsset(value))return value;
   if(assets.Count>=1024)throw new InvalidOperationException("Boss combat asset graph exceeded its bound");
   var copy=Concrete(UObject.Instantiate(script));
   copies.Add(native.Pointer,copy);assets.Add((UObject)copy);
   RewriteFields(value,copy,type);
   if(copy is BossSetupCameraFeature or VerticalScrollingFeature or NogaiToggleCamera)
    ((ActionFeature)copy)._active=false;
   return copy;
  }
  if(Plain(type))
  {
   var copy=Activator.CreateInstance(type);copies.Add(native.Pointer,copy);
   foreach(var p in Fields(type))p.SetValue(copy,Rewrite(p.GetValue(value)));
   if(copy is SendUIEventEffectData ui)
   {
    // Native arena collapse/outro broadcasts must not affect a real encounter.
    // Keep a private stream instead of editing any shared effect/stream asset.
    var id=ui.uiEvent;id.streamCategory="RPCheat-Boss-"+bossEntityVariable?.Pointer.ToString();ui.uiEvent=id;
   }
   return copy;
  }
  if(type.IsGenericType)
  {
   var arg=type.GetGenericArguments()[0];
   if(!typeof(ActionTrigger).IsAssignableFrom(arg)&&!typeof(ActionFeature).IsAssignableFrom(arg)&&!SerializedType(arg))return value;
   var item=type.GetProperty("Item");
   if(item==null)return value;
   if(type.GetGenericTypeDefinition()==typeof(Il2CppSystem.Collections.Generic.List<>))
   {
    int count=(int)type.GetProperty("Count").GetValue(value);
    var copy=Activator.CreateInstance(type);copies.Add(native.Pointer,copy);
    var add=type.GetMethod("Add");
    for(int i=0;i<count;i++)add.Invoke(copy,new[]{Rewrite(item.GetValue(value,new object[]{i}))});
    return copy;
   }
   if(type.GetGenericTypeDefinition()==typeof(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<>))
   {
    long count=Convert.ToInt64(type.GetProperty("Length").GetValue(value));
    var copy=Activator.CreateInstance(type,count);copies.Add(native.Pointer,copy);
    for(long i=0;i<count;i++)
    {var index=Convert.ChangeType(i,item.GetIndexParameters()[0].ParameterType);item.SetValue(copy,Rewrite(item.GetValue(value,new[]{index})),new[]{index});}
    return copy;
   }
  }
  return value;
 }
 private static IEnumerable<PropertyInfo> Fields(Type type)
 {
  for(var t=type;t!=null&&t!=typeof(ScriptableObject)&&t!=typeof(Il2CppSystem.Object);t=t.BaseType)
   foreach(var p in t.GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly))
    if(p.CanRead&&p.CanWrite&&p.GetIndexParameters().Length==0&&t.GetField("NativeFieldInfoPtr_"+p.Name,BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static)!=null)yield return p;
 }
 private void RewriteFields(object source,object copy,Type type)
 {
  foreach(var p in Fields(type))
  {
   var value=p.GetValue(source);var replacement=Rewrite(value);
   if(!ReferenceEquals(value,replacement))p.SetValue(copy,replacement);
  }
 }
 public void Dispose(){for(int i=assets.Count-1;i>=0;i--)if(assets[i]!=null)UObject.Destroy(assets[i]);assets.Clear();copies.Clear();}
}
