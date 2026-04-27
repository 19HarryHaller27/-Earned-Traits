using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace TraitBuffs;

/// <summary>Tier III health consumable; permanent trait requires Tier I + Tier II health lines.</summary>
public class ItemGranolaBarHealth : Item
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

        if (TraitBuffsModSystem.GrantHealthBarTrait(player))
        {
            TraitBuffsModSystem.SendChat(player, "The ration burns slow and warm; your ribs feel armored from within.");
            return;
        }

        if (!TraitBuffsModSystem.HasMeatHeartTrait(player))
        {
            TraitBuffsModSystem.SendChat(player, "You need Health Tier I before this bar can unlock Health Tier III.");
            return;
        }

        if (!TraitBuffsModSystem.HasHealthTrainingTrait(player))
        {
            TraitBuffsModSystem.SendChat(player, "You need Health Tier II before this bar can unlock Health Tier III.");
            return;
        }

        TraitBuffsModSystem.SendChat(player, "Your body already remembers everything this health bar can teach.");
    }
}
