using RoguePrince.CheatMenu;
using UnityEngine.Localization;
int passed=0,failed=0;
void Check(string name,Action action){try{action();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
void Eq(object a,object b){if(!Equals(a,b))throw new Exception($"expected {b}, got {a}");}
var zh=new Locale{Code="zh-CN"};var en=new Locale{Code="en-US"};
Check("description clone keeps numeric variables and arguments without mutating shared reference",()=>{
 var original=new LocalizedString("Items","Affix_A-IncreaseCriticalChance_Description"){LocaleOverride=en,Arguments=new object()};
 var value=new object();original.Add("value",value);
 LocalizedString.Resolve=local=>{Eq(ReferenceEquals(local,original),false);Eq(local.LocaleOverride,zh);Eq(local.Arguments,original.Arguments);Eq(local.m_LocalVariables.Count,1);Eq(local.m_LocalVariables[0].name,"value");Eq(local.m_LocalVariables[0].variable,value);return "<style=highlighted>+10%</style> <style=critical>暴击概率</style>";};
 Eq(LocalizedTextSource.Read(original,zh),"+10% 暴击概率");Eq(original.LocaleOverride,en);Eq(original.m_LocalVariables.Count,1);
});
foreach(var value in new[]{"Items/Affix_A-IncreaseCriticalChance_Name","Affix_A-IncreaseCriticalChance_Description","+{value}% 暴击概率","No translation found for 'Affix_x'", "", "  "})
 Check("unresolved localization is rejected "+value,()=>{LocalizedString.Resolve=_=>value;Eq(LocalizedTextSource.Read(new("Items","Affix_x"),zh),null);});
Check("missing reference and locale are safe",()=>{Eq(LocalizedTextSource.Read(null,zh),null);Eq(LocalizedTextSource.Read(new("Items","x"),null),null);});
Check("native localization exception becomes unavailable text",()=>{LocalizedString.Resolve=_=>throw new IOException("table unavailable");Eq(LocalizedTextSource.Read(new("Items","x"),zh),null);});
Check("English clone uses English locale",()=>{LocalizedString.Resolve=l=>l.LocaleOverride.Code=="en-US"?"+10% Critical Chance":"中文";Eq(LocalizedTextSource.Read(new("Items","Affix_x"),en),"+10% Critical Chance");});
Console.WriteLine($"RESULT {passed} passed, {failed} failed");return failed==0?0:1;
