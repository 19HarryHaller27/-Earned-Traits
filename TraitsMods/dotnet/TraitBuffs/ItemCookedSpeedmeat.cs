using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace TraitBuffs;

/// <summary>
/// Only attached to <c>speedmeat1-cooked</c>. Same food behavior as default Item, then permanent Meat Speed (tier I) on a completed bite.
/// </summary>
public class ItemCookedSpeedmeat : Item
{
    protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        base.tryEatStop(secondsUsed, slot, byEntity);

        if (byEntity is not EntityPlayer eplr || eplr.Player is not IServerPlayer player)
        {
            return;
        }

        if (player.Entity.World.Side != EnumAppSide.Server)
        {
            return;
        }

        if (secondsUsed < 0.5f)
        {
            return;
        }

        if (TraitBuffsModSystem.GrantMeatSpeedTrait(player))
        {
            TraitBuffsModSystem.SendChat(player, "The seasoned meat makes you feel lighter on your feet.");
        }
    }
}
