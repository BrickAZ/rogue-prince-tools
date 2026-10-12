// Test-only native boundary stand-ins; these types never ship in the plugin.
// The actual Transform.forward used by the game is verified separately via
// MethodAddressToToken.db and the installed GameAssembly disassembly.
namespace UnityEngine;
internal struct Vector3 {
    public float x, y, z;
    public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;}
    public static Vector3 operator -(Vector3 a,Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
}
internal sealed class Transform {
    public Vector3 position {get;set;}
    public Vector3 forward {get;set;}
    public Vector3 right {get;set;}
}
