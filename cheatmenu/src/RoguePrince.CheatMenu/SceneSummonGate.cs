// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.EntityFramework;
using UnityEngine;
namespace RoguePrince.CheatMenu;
internal static class SceneSummonGate
{
 internal static bool Block(GameSession game,bool enabled,Entity owner,Vector3 position,Func<Entity,bool,bool> enemy)
 {
  if(!enabled||!Application.isFocused||!game.Ready||owner==null||!float.IsFinite(position.x)||!float.IsFinite(position.y)||!float.IsFinite(position.z)||!enemy(owner,true))return false;
  return game.Player?.GetComponent<RoomStateComponent>()?.currentRoom?.IsInRoom(position)==true;
 }
}
