// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using System.Numerics;
namespace RoguePrince.CheatMenu.Core;
public enum ReceiverShapeKind { Box, Sphere, Capsule }
public readonly record struct ReceiverShape(ReceiverShapeKind Kind,Vector3 Center,Quaternion Rotation,Vector3 Extents,float Radius,float Height,int Axis);
public readonly record struct ReceiverLine(Vector3 First,Vector3 Second);
public static class ReceiverWireframe
{
 public static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
 public static IReadOnlyList<ReceiverLine> Build(ReceiverShape s)
 {
  var result=new List<ReceiverLine>();
  if(!Finite(s.Center)||!float.IsFinite(s.Rotation.LengthSquared())||s.Rotation.LengthSquared()<.000001f)return result;
  var rotation=Quaternion.Normalize(s.Rotation);
  Vector3 World(Vector3 p)=>s.Center+Vector3.Transform(p,rotation);
  void Line(Vector3 a,Vector3 b)=>result.Add(new(World(a),World(b)));
  void Circle(Vector3 center,int plane,float start=0,float angle=MathF.PI*2,int steps=24)
  {
   Vector3 Point(float t)=>center+(plane==0?new Vector3(MathF.Cos(t)*s.Radius,MathF.Sin(t)*s.Radius,0):plane==1?new Vector3(0,MathF.Sin(t)*s.Radius,MathF.Cos(t)*s.Radius):new Vector3(MathF.Cos(t)*s.Radius,0,MathF.Sin(t)*s.Radius));
   var previous=Point(start);for(int i=1;i<=steps;i++){var next=Point(start+angle*i/steps);Line(previous,next);previous=next;}
  }
  if(s.Kind==ReceiverShapeKind.Box)
  {
   var e=s.Extents;if(!Finite(e)||e.X<=0||e.Y<=0||e.Z<=0||e.X>100||e.Y>100||e.Z>100)return result;
   var points=new Vector3[8];for(int i=0;i<8;i++)points[i]=new((i&1)==0?-e.X:e.X,(i&2)==0?-e.Y:e.Y,(i&4)==0?-e.Z:e.Z);
   for(int i=0;i<8;i++)for(int bit=1;bit<=4;bit*=2)if((i&bit)==0)Line(points[i],points[i|bit]);return result;
  }
  if(!float.IsFinite(s.Radius)||s.Radius<=0||s.Radius>100)return result;
  if(s.Kind==ReceiverShapeKind.Sphere){Circle(Vector3.Zero,0);Circle(Vector3.Zero,1);Circle(Vector3.Zero,2);return result;}
  if(s.Kind!=ReceiverShapeKind.Capsule||s.Axis<0||s.Axis>2||!float.IsFinite(s.Height)||s.Height<0||s.Height>200)return result;
  var half=Math.Max(0,s.Height/2-s.Radius);
  if(half==0){Circle(Vector3.Zero,0);Circle(Vector3.Zero,1);Circle(Vector3.Zero,2);return result;}
  // Draw perpendicular rings and two pairs of hemispherical arcs, rather than full circles at the ends.
  rotation=Quaternion.Normalize(rotation*(s.Axis==0?Quaternion.CreateFromAxisAngle(Vector3.UnitZ,-MathF.PI/2):s.Axis==2?Quaternion.CreateFromAxisAngle(Vector3.UnitX,MathF.PI/2):Quaternion.Identity));
  var top=new Vector3(0,half,0);var bottom=-top;
  Circle(top,2);Circle(bottom,2);
  foreach(var plane in new[]{0,1}){Circle(top,plane,0,MathF.PI,12);Circle(bottom,plane,MathF.PI,MathF.PI,12);}
  foreach(var offset in new[]{Vector3.UnitX,-Vector3.UnitX,Vector3.UnitZ,-Vector3.UnitZ})Line(top+offset*s.Radius,bottom+offset*s.Radius);
  return result;
 }
}
