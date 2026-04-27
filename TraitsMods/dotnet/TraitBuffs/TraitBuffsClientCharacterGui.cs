using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace TraitBuffs;

/// <summary>
/// Character dialog panel for earned Trait Buffs traits (runtime <c>characterTraits</c> with this mod's codes).
/// When both mods are installed, Death Wounds composes first (load order); this panel stacks below it.
/// </summary>
public class TraitBuffsClientCharacterGui : ModSystem
{
    public const string ComposerKey = "traitbuffs-additional-traits";

    private const int AdditionalTraitsClipHeight = 320;
    private const int AdditionalTraitsVisibleLineCount = 12;
    private const int ApproxCharsPerRow = 48;
    private ICoreClientAPI? capi;
    private GuiDialogCharacterBase? charDlg;
    private long tickListener;
    private int additionalTraitsLineOffset;
    private List<string> additionalTraitsLines = [];
    private float additionalTraitsLastScrollbarValue = -1f;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;
        tickListener = api.Event.RegisterGameTickListener(OnClientTick, 200);
    }

    public override void Dispose()
    {
        if (capi is not null)
        {
            capi.Event.UnregisterGameTickListener(tickListener);
        }

        DetachFromDialog();
    }

    private void OnClientTick(float dt)
    {
        if (capi is null)
        {
            return;
        }

        if (charDlg is not null)
        {
            if (!IsDialogStillLoaded(charDlg))
            {
                DetachFromDialog();
            }
            else
            {
                SyncAdditionalTraitsFromScrollbar();
                return;
            }
        }

        for (int i = 0; i < capi.Gui.LoadedGuis.Count; i++)
        {
            if (capi.Gui.LoadedGuis[i] is not GuiDialogCharacterBase found)
            {
                continue;
            }

            charDlg = found;
            charDlg.ComposeExtraGuis += OnComposeExtraGuis;
            charDlg.OnClosed += OnCharDialogClosed;
            return;
        }
    }

    private bool IsDialogStillLoaded(GuiDialogCharacterBase dlg)
    {
        if (capi is null)
        {
            return false;
        }

        for (int i = 0; i < capi.Gui.LoadedGuis.Count; i++)
        {
            if (ReferenceEquals(capi.Gui.LoadedGuis[i], dlg))
            {
                return true;
            }
        }

        return false;
    }

    private void OnCharDialogClosed()
    {
        DetachFromDialog();
    }

    private void DetachFromDialog()
    {
        if (charDlg is not null)
        {
            charDlg.ComposeExtraGuis -= OnComposeExtraGuis;
            charDlg.OnClosed -= OnCharDialogClosed;
        }

        charDlg = null;
    }

    private void OnComposeExtraGuis()
    {
        if (capi is null || charDlg is null)
        {
            return;
        }

        var composers = charDlg.Composers;
        if (composers["playercharacter"] is null)
        {
            return;
        }

        ElementBounds left = composers["playercharacter"]!.Bounds;
        ElementBounds? env = composers["environment"]?.Bounds;

        CairoFont bodyFont = CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2);
        additionalTraitsLines = BuildAdditionalTraitsLines(capi);
        additionalTraitsLineOffset = 0;

        const int clipW = 430;
        const int clipH = AdditionalTraitsClipHeight;
        const int yUnderTitle = 28;

        ElementBounds textBounds = ElementBounds.Fixed(0, yUnderTitle, clipW, clipH);
        ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(textBounds);
        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        textBounds = textBounds.WithParent(bgBounds);
        scrollbarBounds = scrollbarBounds.WithParent(bgBounds);
        _ = bgBounds.WithChildren(textBounds, scrollbarBounds);

        double baseOffsetY = env is not null
            ? (env.renderY - left.renderY + env.OuterHeight) / RuntimeEnv.GUIScale + 12
            : (left.OuterHeight / RuntimeEnv.GUIScale) + 8;

        double stackBelowDeath = 0;
        // Same key as Death Wounds mod (no assembly reference — string must stay in sync).
        if (composers.ContainsKey("deathwounds-wounds-panel")
            && composers["deathwounds-wounds-panel"]?.Bounds is { } db)
        {
            stackBelowDeath = (db.OuterHeight + 16) / RuntimeEnv.GUIScale;
        }

        double offsetY = baseOffsetY + stackBelowDeath;

        ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.None)
            .WithFixedPosition(left.renderX / RuntimeEnv.GUIScale, left.renderY / RuntimeEnv.GUIScale + offsetY);

        const string dynamicTextKey = "traitbuffstext";
        const string scrollbarKey = "traitbuffs-sb";
        var compo = capi.Gui
            .CreateCompo(ComposerKey, dialogBounds)
            .AddShadedDialogBG(bgBounds, true, 0, 0.5f)
            .AddDialogTitleBar(Lang.Get("traitbuffs:gui-title-additional-traits"), () => charDlg?.TryClose())
            .BeginChildElements(bgBounds)
            .AddDynamicText(ComposeVisibleTraitsLines(), bodyFont, textBounds, dynamicTextKey)
            .AddVerticalScrollbar(OnAdditionalTraitsScroll, scrollbarBounds, scrollbarKey)
            .EndChildElements()
            .Compose();

        if (compo.GetScrollbar(scrollbarKey) is { } sc)
        {
            sc.SetHeights(AdditionalTraitsVisibleLineCount, Math.Max(additionalTraitsLines.Count, AdditionalTraitsVisibleLineCount));
            additionalTraitsLastScrollbarValue = sc.CurrentYPosition * sc.ScrollConversionFactor;
        }

        composers[ComposerKey] = compo;
    }

    private void OnAdditionalTraitsScroll(float newValue)
    {
        if (capi is null)
        {
            return;
        }

        GuiComposer? composer = charDlg?.Composers?[ComposerKey];
        if (composer is null)
        {
            return;
        }

        int maxOffset = Math.Max(0, additionalTraitsLines.Count - AdditionalTraitsVisibleLineCount);
        float raw = newValue;
        if (raw <= 1.001f && maxOffset > 1)
        {
            raw *= maxOffset;
        }

        additionalTraitsLineOffset = (int)Math.Clamp(MathF.Round(raw), 0, maxOffset);
        UpdateAdditionalTraitsText(composer);
    }

    private void SyncAdditionalTraitsFromScrollbar()
    {
        GuiComposer? composer = charDlg?.Composers?[ComposerKey];
        if (composer is null)
        {
            return;
        }

        if (composer.GetScrollbar("traitbuffs-sb") is not { } sc)
        {
            return;
        }

        float raw = sc.CurrentYPosition * sc.ScrollConversionFactor;
        if (MathF.Abs(raw - additionalTraitsLastScrollbarValue) < 0.001f)
        {
            return;
        }

        additionalTraitsLastScrollbarValue = raw;
        int maxOffset = Math.Max(0, additionalTraitsLines.Count - AdditionalTraitsVisibleLineCount);
        if (raw <= 1.001f && maxOffset > 1)
        {
            raw *= maxOffset;
        }

        int newOffset = (int)Math.Clamp(MathF.Round(raw), 0, maxOffset);
        if (newOffset == additionalTraitsLineOffset)
        {
            return;
        }

        additionalTraitsLineOffset = newOffset;
        UpdateAdditionalTraitsText(composer);
    }

    private void UpdateAdditionalTraitsText(GuiComposer composer)
    {
        if (composer.GetDynamicText("traitbuffstext") is { } txt)
        {
            txt.SetNewText(ComposeVisibleTraitsLines(), false, true, false);
        }
    }

    private string ComposeVisibleTraitsLines()
    {
        if (additionalTraitsLines.Count == 0)
        {
            return Lang.Get("traitbuffs:gui-additional-traits-empty");
        }

        int start = Math.Clamp(additionalTraitsLineOffset, 0, Math.Max(0, additionalTraitsLines.Count - 1));
        int count = Math.Min(AdditionalTraitsVisibleLineCount, additionalTraitsLines.Count - start);
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                sb.Append('\n');
            }

            sb.Append(additionalTraitsLines[start + i]);
        }

        return sb.ToString();
    }

    private static bool IsEarnedTraitBuffCode(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        return code.Contains("traitbuffs:", StringComparison.OrdinalIgnoreCase)
            || code.Contains("traitbuffs-", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> BuildAdditionalTraitsLines(ICoreClientAPI capi)
    {
        SyncedTreeAttribute? attrs = capi.World.Player?.Entity.WatchedAttributes;
        var lines = new List<string>(32);

        string[]? list = attrs?.GetStringArray("characterTraits");
        if (list is not null)
        {
            for (int i = 0; i < list.Length; i++)
            {
                string? code = list[i];
                if (!IsEarnedTraitBuffCode(code))
                {
                    continue;
                }

                string shortId = code.Contains(':', StringComparison.Ordinal)
                    ? code[(code.IndexOf(":", StringComparison.Ordinal) + 1)..]
                    : code;

                string nameKey = "game:traitname-" + shortId;
                string descKey = "game:traitdesc-" + shortId;
                string name = Lang.HasTranslation(nameKey) ? Lang.Get(nameKey) : shortId;
                string desc = Lang.HasTranslation(descKey) ? Lang.Get(descKey) : "";
                AddWrappedLine(lines, "• " + name);
                if (desc.Length > 0)
                {
                    AddWrappedLine(lines, desc);
                }

                lines.Add("");
            }
        }

        if (lines.Count == 0)
        {
            lines.Add(Lang.Get("traitbuffs:gui-additional-traits-empty"));
        }

        return lines;
    }

    private static void AddWrappedLine(List<string> rows, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            rows.Add("");
            return;
        }

        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (current.Length == 0)
            {
                current.Append(word);
                continue;
            }

            if (current.Length + 1 + word.Length > ApproxCharsPerRow)
            {
                rows.Add(current.ToString());
                current.Clear();
                current.Append(word);
            }
            else
            {
                current.Append(' ');
                current.Append(word);
            }
        }

        if (current.Length > 0)
        {
            rows.Add(current.ToString());
        }
    }
}
