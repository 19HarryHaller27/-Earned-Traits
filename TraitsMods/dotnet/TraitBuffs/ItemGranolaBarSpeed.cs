using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace TraitBuffs;

/// <summary>Tier III speed consumable. Eating still feeds normally; permanent trait is gated behind Tier I + Tier II.</summary>
public class ItemGranolaBarSpeed : Item
{
    protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        base.tryEatStop(secondsUsed, slot, byEntity);

        if (secondsUsed < 0.5f || byEntity is not EntityPlayer eplr || eplr.Player is not IServerPlayer player)
        {
            return;
        }

        if (player.Entity.World.Side != EnumAppSide.Server)
        {
            return;
        }

        if (TraitBuffsModSystem.GrantSpeedBarTrait(player))
        {
            TraitBuffsModSystem.SendChat(player, "A dense rush of energy settles into your stride. You feel unmistakably faster.");
            return;
        }

        if (!TraitBuffsModSystem.HasMeatSpeedTrait(player))
        {
            TraitBuffsModSystem.SendChat(player, "You need Speed Tier I before this bar can unlock Speed Tier III.");
            return;
        }

        if (!TraitBuffsModSystem.HasSpeedTrait(player))
        {
            TraitBuffsModSystem.SendChat(player, "You need Speed Tier II before this bar can unlock Speed Tier III.");
            return;
        }

        TraitBuffsModSystem.SendChat(player, "Your body already remembers everything this speed bar can teach.");
    }
}
