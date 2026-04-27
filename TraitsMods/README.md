# Earned Traits — Barebones Framework

| | |
|-|-|
| **Mod id** | `traitbuffs` |
| **Name** | Earned Traits (in-game: see `modinfo.json` **name**) |
| **Version** | 1.0.0 |
| **Author** | [Squidbat on ModDB](https://mods.vintagestory.at/show/mod/47309) |
| **Game** | [Vintage Story 1.22.0+](https://mods.vintagestory.at/show/mod/47309) (recommended: 1.22) |
| **.NET (build)** | 10+ (`net10.0`) |

**Official mod page and downloads (players):** [mods.vintagestory.at — mod 47309](https://mods.vintagestory.at/show/mod/47309) (recommended download: `TraitBuffs.zip`, 1-click install for 1.22.0)

---

## What it is

*Designed for use as a barebones framework for modders to flesh out!*

**Trait Buffs** ships a small, working example of an **earned trait** loop: **special foods and manuals** grant **permanent** bonuses and show up in a **Character** dialog panel. The real goal is to hand you a clean layout—**JSON** traits, item types, **recipes**, grid/cooking chains, and **C#** that talks to `characterTraits` and `EntityStats` / max health—so you can **swap in your own** items, tiers, and fiction without starting from zero.

## Purpose

- Show one pattern for **gated** stat progression (e.g. meat → manual → bar).
- **GUI** that will show related stats in Character.

## For modders (how to use this as a template)

- **Duplicate** the mod folder, change `modid` and domain in `modinfo.json` and asset paths, then rename **trait codes** in `config/traits.json` and `lang/en.json` **together**.
- Item behavior lives in small `Item` subclasses; trait grants live in one `ModSystem`—trace from **recipes** → item JSON `class` → C#.
- **Extend** the Character GUI class if you want different copy or extra sections.
- **Playable on vanilla** as-is, but the design is a **framework** to mod your own flavortext, stats, and crafting.
- **Works on 1.22 vanilla**; not heavily tested with other mods.
- Stats are easy to tweak in JSON/C# as needed.

## Repository layout

This Git repository is **one mod only** (`traitbuffs` domain).

```text
modinfo.json
assets/traitbuffs/     JSON: traits, item types, recipes, lang, config, …
dotnet/TraitBuffs/     C#: TraitBuffs.csproj, items, ModSystem, character GUI
Directory.Build.props
```

Build: **`dotnet build "dotnet\TraitBuffs\TraitBuffs.csproj" -c Release`**. Set your **game** path in `Directory.Build.props` (or `VINTAGE_STORY_PATH` / `-p:VintageStoryPath=...`).

Build output also populates `dist/traitbuffs` and the mod root; **`dist/`, `*.dll`, `*.pdb` are gitignored**—rebuild after clone.

---

## License / use (as on ModDB)

> Free to use, copy, modify, and redistribute this mod and **derived** works for **any purpose** (including commercial packs), **provided** you **credit the original author (Squidbat)** in your **README**, ModDB page, or in-repo credits—enough that players and other modders know where the **base** came from for help, **bug reports**, and **compatibility**. **Provide a link to** the [ModDB mod page / author listing](https://mods.vintagestory.at/show/mod/47309) (or your ModDB author profile) as a courtesy.  
> This is a **community courtesy** description from ModDB, not a substitute for legal advice; a formal OSS license (MIT) is in [LICENSE](LICENSE) in addition to that notice when you work from this source tree.

---

*Tags on ModDB: mechanics, gameplay · **Side:** both · matching listing: [47309](https://mods.vintagestory.at/show/mod/47309)*
