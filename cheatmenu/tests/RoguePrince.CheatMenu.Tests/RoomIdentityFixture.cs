// Test-only IL2CPP boundary stand-ins; never compiled into the plugin.
// The real currentRoom getter's boxing call is preserved in native-room-getter.txt.
internal sealed class LevelRoomData {
    internal IntPtr Pointer;
    internal RoomTemplate template;
    internal (int x, int y) position;
}
internal sealed class RoomTemplate { internal IntPtr Pointer; }
internal static class RoomIdentityFixture {
    internal static LevelRoomData Room(int box,int template,int x,int y) => new() {
        Pointer=new IntPtr(box),template=new RoomTemplate{Pointer=new IntPtr(template)},position=(x,y)
    };
}
