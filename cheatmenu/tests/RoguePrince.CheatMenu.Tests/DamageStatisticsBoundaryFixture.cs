// Minimal read-only native surface for the linked production statistics source.
namespace UnityEngine
{
    internal static class Application{internal static bool isFocused=true;}
    internal static class Time{internal static int frameCount=1;}
    internal struct Scene{internal int handle;}
    internal sealed class GameObject{internal bool activeInHierarchy=true;internal Scene scene=new(){handle=1};}
}
internal class BaseDamageCaster{internal T TryCast<T>()where T:class=>this as T;}
internal sealed class DamageCaster:BaseDamageCaster{}
internal sealed class DirectDamageCaster:BaseDamageCaster{}
