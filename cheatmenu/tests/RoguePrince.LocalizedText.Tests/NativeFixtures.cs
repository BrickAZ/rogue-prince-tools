// Minimal localization API fixture. No Unity or native runtime behavior is simulated.
namespace UnityEngine.Localization
{
 public sealed class Locale { public string Code; }
 public sealed class LocalizedString
 {
  public string TableReference,TableEntryReference;
  public Locale LocaleOverride;
  public object Arguments;
  public readonly List<VariablePair> m_LocalVariables=new();
  public LocalizedString(string table,string entry){TableReference=table;TableEntryReference=entry;}
  public void Add(string name,object variable)=>m_LocalVariables.Add(new(){name=name,variable=variable});
  public static Func<LocalizedString,string> Resolve;
  public string GetLocalizedString()=>Resolve(this);
 }
 public sealed class VariablePair { public string name; public object variable; }
}
