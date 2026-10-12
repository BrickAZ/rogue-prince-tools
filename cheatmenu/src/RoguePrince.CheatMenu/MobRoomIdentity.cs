// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu;

internal static class MobRoomIdentity
{
    // LevelRoomData is a native value type: currentRoom boxes a fresh copy.
    // Its wrapper Pointer is not room identity. RoomTemplate is a reference
    // asset; template + placed coordinates identify the room within a scene.
    // Generation and scene are checked separately by MobSpawnActions.
    internal static bool Matches(LevelRoomData expected, LevelRoomData actual) =>
        expected?.template != null && actual?.template != null &&
        expected.template.Pointer == actual.template.Pointer &&
        expected.position.x == actual.position.x &&
        expected.position.y == actual.position.y;
}
