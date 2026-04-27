using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace TraitBuffs;

public class TraitBuffsModSystem : ModSystem
{
    internal const string SpeedTraitCode = "traitbuffs:traitbuffs-speedtraining";
    internal const string SpeedTraitCodeLegacyShort = "traitbuffs-speedtraining";
    internal const string SpeedTraitCodeLegacyFromTraitCore = "traitcore:traitcore-speedtraining";
    internal const string SpeedTraitCodeLegacyFromTraitCoreShort = "traitcore-speedtraining";
    internal const string SpeedBuffStatCode = "traitbuffs-speedtraining";

    internal const string MeatSpeedTraitCode = "traitbuffs:traitbuffs-meatspeed";
    internal const string MeatSpeedTraitCodeFromTraitCore = "traitcore:traitcore-meatspeed";
    internal const string MeatSpeedBuffStatCode = "traitbuffs-meatspeed";

    internal const string SpeedBarTraitCode = "traitbuffs:traitbuffs-speedbar";
    internal const string SpeedBarTraitCodeFromTraitCore = "traitcore:traitcore-speedbar";
    internal const string SpeedBarBuffStatCode = "traitbuffs-speedbar";

    internal const string HealthTrainingTraitCode = "traitbuffs:traitbuffs-healthtraining";
    internal const string HealthTrainingTraitCodeFromTraitCore = "traitcore:traitcore-healthtraining";
    internal const string MeatHeartTraitCode = "traitbuffs:traitbuffs-meathp";
    internal const string MeatHeartTraitCodeFromTraitCore = "traitcore:traitcore-meathp";
    internal const string HealthBarTraitCode = "traitbuffs:traitbuffs-healthbar";
    internal const string HealthBarTraitCodeFromTraitCore = "traitcore:traitcore-healthbar";

    internal const string MaxHpKeyTier1 = "traitbuffs-maxhp-tier1";
    internal const string MaxHpKeyTier2 = "traitbuffs-maxhp-tier2";
    internal const string MaxHpKeyTier3 = "traitbuffs-maxhp-tier3";
    internal const string HealEffectTier1 = "traitbuffs-healeff-tier1";
    internal const string HealEffectTier2 = "traitbuffs-healeff-tier2";
    internal const string HealEffectTier3 = "traitbuffs-healeff-tier3";

    private static readonly (string From, string To)[] TraitCoreMigrationMap =
    {
        (MeatSpeedTraitCodeFromTraitCore, MeatSpeedTraitCode),
        (SpeedTraitCodeLegacyFromTraitCore, SpeedTraitCode),
        (SpeedBarTraitCodeFromTraitCore, SpeedBarTraitCode),
        (MeatHeartTraitCodeFromTraitCore, MeatHeartTraitCode),
        (HealthTrainingTraitCodeFromTraitCore, HealthTrainingTraitCode),
        (HealthBarTraitCodeFromTraitCore, HealthBarTraitCode),
    };

    private ICoreServerAPI? sapi;

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("ItemTrainingManualSpeed", typeof(ItemTrainingManualSpeed));
        api.RegisterItemClass("ItemCookedSpeedmeat", typeof(ItemCookedSpeedmeat));
        api.RegisterItemClass("ItemGranolaBarSpeed", typeof(ItemGranolaBarSpeed));
        api.RegisterItemClass("ItemTrainingManualHealth", typeof(ItemTrainingManualHealth));
        api.RegisterItemClass("ItemCookedHealthmeat", typeof(ItemCookedHealthmeat));
        api.RegisterItemClass("ItemGranolaBarHealth", typeof(ItemGranolaBarHealth));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;
        api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
        api.Event.PlayerRespawn += OnPlayerRespawn;
    }

    public override void Dispose()
    {
        if (sapi != null)
        {
            sapi.Event.PlayerNowPlaying -= OnPlayerNowPlaying;
            sapi.Event.PlayerRespawn -= OnPlayerRespawn;
        }
    }

    private void OnPlayerNowPlaying(IServerPlayer player)
    {
        TryMigrateTraitCodesFromTraitCore(player);
        TryMigrateSpeedTraitId(player);
        ClearLegacyMaxHpKeys(player);
        EnsureSpeedBuffApplied(player);
        EnsureMeatSpeedBuffApplied(player);
        EnsureSpeedBarBuffApplied(player);
        EnsureHealthLineBuffs(player);
    }

    private void OnPlayerRespawn(IServerPlayer player)
    {
        TryMigrateTraitCodesFromTraitCore(player);
        TryMigrateSpeedTraitId(player);
        ClearLegacyMaxHpKeys(player);
        EnsureSpeedBuffApplied(player);
        EnsureMeatSpeedBuffApplied(player);
        EnsureSpeedBarBuffApplied(player);
        EnsureHealthLineBuffs(player);
    }

    /// <summary>One-time rename of character trait strings from the old combined <c>traitcore</c> mod.</summary>
    private static void TryMigrateTraitCodesFromTraitCore(IServerPlayer player)
    {
        string[]? traits = player.Entity.WatchedAttributes.GetStringArray("characterTraits");
        if (traits is null || traits.Length == 0)
        {
            return;
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach ((string from, string to) in TraitCoreMigrationMap)
        {
            map[from] = to;
        }

        bool dirty = false;
        for (int i = 0; i < traits.Length; i++)
        {
            if (traits[i] is not { } t || string.IsNullOrEmpty(t))
            {
                continue;
            }

            if (map.TryGetValue(t, out string? replacement))
            {
                traits[i] = replacement;
                dirty = true;
            }
        }

        if (dirty)
        {
            player.Entity.WatchedAttributes.SetStringArray("characterTraits", traits);
            player.Entity.WatchedAttributes.MarkPathDirty("characterTraits");
        }
    }

    private static void ClearLegacyMaxHpKeys(IServerPlayer player)
    {
        var e = player.Entity;
        foreach (string k in new[] { "traitcore-maxhp-tier1", "traitcore-maxhp-tier2", "traitcore-maxhp-tier3" })
        {
            EntityHealthBonus.ClearFlatMaxHpBonus(e, k);
        }
    }

    private static void TryMigrateSpeedTraitId(IServerPlayer player)
    {
        string[]? traits = player.Entity.WatchedAttributes.GetStringArray("characterTraits");
        if (traits == null || traits.Length == 0)
        {
            return;
        }

        int legacyIdx = Array.FindIndex(
            traits,
            s => string.Equals(s, SpeedTraitCodeLegacyShort, StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, SpeedTraitCodeLegacyFromTraitCoreShort, StringComparison.OrdinalIgnoreCase));

        if (legacyIdx < 0)
        {
            return;
        }

        if (Array.Exists(traits, s => string.Equals(s, SpeedTraitCode, StringComparison.OrdinalIgnoreCase)))
        {
            var pruned = new string[traits.Length - 1];
            int w = 0;
            for (int i = 0; i < traits.Length; i++)
            {
                if (i != legacyIdx)
                {
                    pruned[w++] = traits[i];
                }
            }

            player.Entity.WatchedAttributes.SetStringArray("characterTraits", pruned);
        }
        else
        {
            var updated = (string[])traits.Clone();
            updated[legacyIdx] = SpeedTraitCode;
            player.Entity.WatchedAttributes.SetStringArray("characterTraits", updated);
        }

        player.Entity.WatchedAttributes.MarkPathDirty("characterTraits");
    }

    internal static bool HasSpeedTrait(IServerPlayer player)
    {
        string[] traits = player.Entity.WatchedAttributes.GetStringArray("characterTraits") ?? Array.Empty<string>();
        for (int i = 0; i < traits.Length; i++)
        {
            if (string.Equals(traits[i], SpeedTraitCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(traits[i], SpeedTraitCodeLegacyFromTraitCore, StringComparison.OrdinalIgnoreCase)
                || string.Equals(traits[i], SpeedTraitCodeLegacyShort, StringComparison.OrdinalIgnoreCase)
                || string.Equals(traits[i], SpeedTraitCodeLegacyFromTraitCoreShort, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool GrantSpeedTrait(IServerPlayer player)
    {
        if (!HasMeatSpeedTrait(player))
        {
            return false;
        }

        if (HasSpeedTrait(player)) return false;

        string[] traits = player.Entity.WatchedAttributes.GetStringArray("characterTraits") ?? Array.Empty<string>();
        var newTraits = new string[traits.Length + 1];
        Array.Copy(traits, newTraits, traits.Length);
        newTraits[^1] = SpeedTraitCode;

        player.Entity.WatchedAttributes.SetStringArray("characterTraits", newTraits);
        player.Entity.WatchedAttributes.MarkPathDirty("characterTraits");

        ApplySpeedBuff(player);
        return true;
    }

    internal static void ApplySpeedBuff(IServerPlayer player)
    {
        player.Entity.Stats.Set("walkspeed", SpeedBuffStatCode, 1f, true);
        player.Entity.Stats.Set("sprintSpeed", SpeedBuffStatCode, 1f, true);
    }

    private static void EnsureSpeedBuffApplied(IServerPlayer player)
    {
        if (HasSpeedTrait(player))
        {
            ApplySpeedBuff(player);
        }
    }

    internal static bool HasMeatSpeedTrait(IServerPlayer player)
    {
        string[] traits = player.Entity.WatchedAttributes.GetStringArray("characterTraits") ?? Array.Empty<string>();
        for (int i = 0; i < traits.Length; i++)
        {
            if (string.Equals(traits[i], MeatSpeedTraitCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(traits[i], MeatSpeedTraitCodeFromTraitCore, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool GrantMeatSpeedTrait(IServerPlayer player)
    {
        if (HasMeatSpeedTrait(player))
        {
            return false;
        }

        string[] traits = player.Entity.WatchedAttributes.GetStringArray("characterTraits") ?? Array.Empty<string>();
        var newTraits = new string[traits.Length + 1];
        Array.Copy(traits, newTraits, traits.Length);
        newTraits[^1] = MeatSpeedTraitCode;

        player.Entity.WatchedAttributes.SetStringArray("characterTraits", newTraits);
        player.Entity.WatchedAttributes.MarkPathDirty("characterTraits");

        ApplyMeatSpeedBuff(player);
        return true;
    }

    internal static void ApplyMeatSpeedBuff(IServerPlayer player)
    {
        player.Entity.Stats.Set("walkspeed", MeatSpeedBuffStatCode, 1f, true);
        player.Entity.Stats.Set("sprintSpeed", MeatSpeedBuffStatCode, 1f, true);
    }

    private static void EnsureMeatSpeedBuffApplied(IServerPlayer player)
    {
        if (HasMeatSpeedTrait(player))
        {
            ApplyMeatSpeedBuff(player);
        }
    }

    internal static bool HasSpeedBarTrait(IServerPlayer player)
    {
        string[] traits = player.Entity.WatchedAttributes.GetStringArray("characterTraits") ?? Array.Empty<string>();
        for (int i = 0; i < traits.Length; i++)
        {
            if (string.Equals(traits[i], SpeedBarTraitCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(traits[i], SpeedBarTraitCodeFromTraitCore, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool GrantSpeedBarTrait(IServerPlayer player)
    {
        if (!HasMeatSpeedTrait(player) || !HasSpeedTrait(player) || HasSpeedBarTrait(player))
        {
            return false;
        }

        string[] traits = player.Entity.WatchedAttributes.GetStringArray("characterTraits") ?? Array.Empty<string>();
        var newTraits = new string[traits.Length + 1];
        Array.Copy(traits, newTraits, traits.Length);
        newTraits[^1] = SpeedBarTraitCode;

        player.Entity.WatchedAttributes.SetStringArray("characterTraits", newTraits);
        player.Entity.WatchedAttributes.MarkPathDirty("characterTraits");
        ApplySpeedBarBuff(player);
        return true;
    }

    internal static void ApplySpeedBarBuff(IServerPlayer player)
    {
        player.Entity.Stats.Set("walkspeed", SpeedBarBuffStatCode, 1f, true);
        player.Entity.Stats.Set("sprintSpeed", SpeedBarBuffStatCode, 1f, true);
    }

    private static void EnsureSpeedBarBuffApplied(IServerPlayer player)
    {
        if (HasSpeedBarTrait(player))
        {
            ApplySpeedBarBuff(player);
        }
    }

    internal static bool HasMeatHeartTrait(IServerPlayer player) =>
        HasTrait(player, MeatHeartTraitCode) || HasTrait(player, MeatHeartTraitCodeFromTraitCore);

    internal static bool GrantMeatHeartTrait(IServerPlayer player)
    {
        if (HasMeatHeartTrait(player))
        {
            return false;
        }

        AppendTrait(player, MeatHeartTraitCode);
        ApplyHealthTierBuffs(player);
        return true;
    }

    internal static bool HasHealthTrainingTrait(IServerPlayer player) =>
        HasTrait(player, HealthTrainingTraitCode) || HasTrait(player, HealthTrainingTraitCodeFromTraitCore);

    internal static bool GrantHealthTrainingTrait(IServerPlayer player)
    {
        if (!HasMeatHeartTrait(player) || HasHealthTrainingTrait(player))
        {
            return false;
        }

        AppendTrait(player, HealthTrainingTraitCode);
        ApplyHealthTierBuffs(player);
        return true;
    }

    internal static bool HasHealthBarTrait(IServerPlayer player) =>
        HasTrait(player, HealthBarTraitCode) || HasTrait(player, HealthBarTraitCodeFromTraitCore);

    internal static bool GrantHealthBarTrait(IServerPlayer player)
    {
        if (!HasMeatHeartTrait(player) || !HasHealthTrainingTrait(player) || HasHealthBarTrait(player))
        {
            return false;
        }

        AppendTrait(player, HealthBarTraitCode);
        ApplyHealthTierBuffs(player);
        return true;
    }

    private static bool HasTrait(IServerPlayer player, string fullCode)
    {
        string[] traits = player.Entity.WatchedAttributes.GetStringArray("characterTraits") ?? Array.Empty<string>();
        for (int i = 0; i < traits.Length; i++)
        {
            if (string.Equals(traits[i], fullCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendTrait(IServerPlayer player, string fullCode)
    {
        string[] traits = player.Entity.WatchedAttributes.GetStringArray("characterTraits") ?? Array.Empty<string>();
        var newTraits = new string[traits.Length + 1];
        Array.Copy(traits, newTraits, traits.Length);
        newTraits[^1] = fullCode;
        player.Entity.WatchedAttributes.SetStringArray("characterTraits", newTraits);
        player.Entity.WatchedAttributes.MarkPathDirty("characterTraits");
    }

    internal static void ApplyHealthTierBuffs(IServerPlayer player)
    {
        var e = player.Entity;
        EntityHealthBonus.ClearFlatMaxHpBonus(e, MaxHpKeyTier1);
        EntityHealthBonus.ClearFlatMaxHpBonus(e, MaxHpKeyTier2);
        EntityHealthBonus.ClearFlatMaxHpBonus(e, MaxHpKeyTier3);
        e.Stats.Remove("healingeffectivness", HealEffectTier1);
        e.Stats.Remove("healingeffectivness", HealEffectTier2);
        e.Stats.Remove("healingeffectivness", HealEffectTier3);

        if (HasMeatHeartTrait(player))
        {
            EntityHealthBonus.SetFlatMaxHpBonus(e, MaxHpKeyTier1, 2f);
            e.Stats.Set("healingeffectivness", HealEffectTier1, 0.04f, true);
        }

        if (HasHealthTrainingTrait(player))
        {
            EntityHealthBonus.SetFlatMaxHpBonus(e, MaxHpKeyTier2, 2f);
            e.Stats.Set("healingeffectivness", HealEffectTier2, 0.04f, true);
        }

        if (HasHealthBarTrait(player))
        {
            EntityHealthBonus.SetFlatMaxHpBonus(e, MaxHpKeyTier3, 4f);
            e.Stats.Set("healingeffectivness", HealEffectTier3, 0.08f, true);
        }
    }

    private static void EnsureHealthLineBuffs(IServerPlayer player)
    {
        if (HasMeatHeartTrait(player) || HasHealthTrainingTrait(player) || HasHealthBarTrait(player))
        {
            ApplyHealthTierBuffs(player);
        }
    }

    internal static void SendChat(IServerPlayer player, string message)
    {
        player.SendMessage(GlobalConstants.GeneralChatGroup, message, EnumChatType.CommandSuccess);
    }
}
