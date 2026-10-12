namespace MotherBase.ActionFramework;
using RoguePrince.CheatMenu;
internal class ActionBindingsReference{internal ActionBindingsOverride overrideBindings;internal ActionBindingsReference(Player p)=>overrideBindings=new(p);}
internal class ActionBindingsOverride{internal ActionBindingsOverride(Player p){}internal Il2CppSystem.Collections.Generic.List<ItemFeature> overriders=new();}
