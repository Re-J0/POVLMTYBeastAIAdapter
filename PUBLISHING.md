# Publishing checklist

This repository includes source and packaging helpers for GitHub, Thunderstore/Gale, and Nexus Mods.

## 1. Before publishing

- Verify that the public GitHub/Nexus/Thunderstore identity and project links are correct before publishing.
- Build `POVLMTYBeastAIAdapter.dll` from the exact source/tag you are publishing.
- Do **not** bundle PathOfValheiman, LetMeTameYou, Valheim game assemblies, or BepInEx binaries.
- Keep the version in `Plugin.cs`, Thunderstore `manifest.json`, release tag, and uploaded file name synchronized.
- This project is licensed under the **MIT License**. Redistribution and modified redistribution are allowed as long as the MIT copyright and permission notice are preserved.
- Review third-party mod licenses/permissions before publishing compatibility work that interacts with them.

## 2. GitHub

Repository:

```text
https://github.com/Re-J0/POVLMTYBeastAIAdapter
```

First release/tag:

```text
v0.3.1
```

Recommended release assets:

```text
POVLMTYBeastAIAdapter-0.3.1.dll
POVLMTYBeastAIAdapter-0.3.1-Thunderstore.zip
POVLMTYBeastAIAdapter-0.3.1-Nexus.zip
POVLMTYBeastAIAdapter-0.3.1-Source.zip
```

Use `RELEASE_NOTES_0.3.1.md` as the first GitHub Release description.

## 3. Thunderstore and Gale

Gale consumes Thunderstore packages, so a valid Valheim Thunderstore release can be installed through Gale without maintaining a separate Gale-specific package.

The Thunderstore package root must contain:

```text
manifest.json
README.md
icon.png            # exactly 256x256 PNG
CHANGELOG.md         # optional but included here
LICENSE
plugins/
  POVLMTYBeastAIAdapter/
    POVLMTYBeastAIAdapter.dll
```

The repository icon is stored at both `icon.png` and `distribution/thunderstore/icon.png`.

The `manifest.json` template in `distribution/thunderstore/` pins the versions used for final validation:

```text
denikson-BepInExPack_Valheim-5.4.2351
treextr-PathOfValheiman-4.13.10
Meldurson-LetMeTameYou-0.1.0
```

Run:

```powershell
./scripts/package_thunderstore.ps1
```

After packaging, open the ZIP and verify that `manifest.json`, `README.md`, `icon.png`, `CHANGELOG.md`, `LICENSE`, and `plugins/` are directly at the ZIP root, not inside another containing folder.

### Thunderstore category disclosure

If the Thunderstore uploader provides an **AI Generated** category for substantially AI-assisted projects, select it for this project. The implementation was developed with substantial AI assistance and then validated through repeated runtime testing.

Also choose the normal Valheim category/categories that best describe the package, such as utility/mod/compatibility categories if available.

## 4. Nexus Mods

Before publishing, review the current Nexus submission rules and the upstream projects' license/permission terms. This adapter exists specifically to interoperate with PathOfValheiman and LetMeTameYou, so keep clear credits and dependency links for both projects.

If Nexus or an upstream author requires explicit approval for this compatibility release, obtain and retain written permission before publishing there.

Once ready, run:

```powershell
./scripts/package_nexus.ps1
```

The produced archive contains:

```text
BepInEx/plugins/POVLMTYBeastAIAdapter/POVLMTYBeastAIAdapter.dll
README.md
LICENSE
```

On the Nexus mod page:

- game: Valheim
- main file: `POVLMTYBeastAIAdapter-0.3.1-Nexus.zip`
- required mods/resources: BepInExPack Valheim, PathOfValheiman, LetMeTameYou
- do not upload copies of those dependencies inside your archive
- use `NEXUS_DESCRIPTION.md` as the page description
- disclose the substantial AI-assisted development accurately if the uploader provides an applicable field/tag
- set redistribution permissions consistently with the MIT License

## 5. Version updates

For a future `0.3.2`:

1. update `PluginVersion` in `Plugin.cs`;
2. update `version_number` in `distribution/thunderstore/manifest.json`;
3. add a `CHANGELOG.md` entry;
4. build and validate the DLL;
5. create a Git tag/release;
6. package new Thunderstore/Nexus archives;
7. upload the new Thunderstore version under the **same package name/team**.

Do not change the Thunderstore package `name` merely to publish an update; changing it creates a different package identity.
