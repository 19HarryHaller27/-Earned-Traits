using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace TraitBuffs;

/// <summary><c>healthmeat1-cooked</c>: normal food, then permanent Meat Heart (tier I) once per character.</summary>
public class ItemCookedHealthmeat : Item
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

        if (TraitBuffsModSystem.GrantMeatHeartTrait(player))
        {
            TraitBuffsModSystem.SendChat(player, "The rich meat settles deep; your pulse feels steadier, your frame a little hardier.");
        }
    }
}
