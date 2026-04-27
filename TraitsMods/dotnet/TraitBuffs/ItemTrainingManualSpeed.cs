using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace TraitBuffs;

public class ItemTrainingManualSpeed : Item
{
    public override void OnHeldInteractStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handling)
    {
        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);

        handling = EnumHandHandling.PreventDefault;

        if (!firstEvent || byEntity?.World == null || byEntity.World.Side != EnumAppSide.Server)
        {
            return;
        }

        if (byEntity is not EntityPlayer eplr || eplr.Player is not IServerPlayer player)
        {
            return;
        }

        bool added = TraitBuffsModSystem.GrantSpeedTrait(player);
        if (added)
        {
            TraitBuffsModSystem.SendChat(player, "You feel faster.");
        }
        else if (!TraitBuffsModSystem.HasMeatSpeedTrait(player))
        {
            TraitBuffsModSystem.SendChat(player, "You must first earn Speed Tier I before this manual can teach Tier II.");
        }
        else
        {
            TraitBuffsModSystem.SendChat(player, "You've learned as much as you can from this Training Manual.");
        }
    }
}
