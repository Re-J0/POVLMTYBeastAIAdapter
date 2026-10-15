# Publishing checklist

This repository includes source and packaging helpers for GitHub, Thunderstore/Gale, and Nexus Mods.

## 1. Before publishing

- Verify that the public GitHub/Nexus/Thunderstore identity and project links are correct before publishing.
- Build `POVLMTYBeastAIAdapter.dll` from the exact source/tag you are publishing.
- Do **not** bundle PathOfValheiman, LetMeTameYou, Valheim game assemblies, or BepInEx binaries.
- Keep the version in `Plugin.cs`, Thunderstore `manifest.json`, release tag, and uploaded file name synchronized.
- Review third-party mod licenses/permissions before publishing compatibility work that interacts with them.

## 2. GitHub

Recommended repository name:

```text
POVLMTYBeastAIAdapter
```

Recommended first release/tag:

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
plugins/
  POVLMTYBeastAIAdapter/
    POVLMTYBeastAIAdapter.dll
```

The `manifest.json` template in `distribution/thunderstore/` pins the versions that were used for final validation:

```text
denikson-BepInExPack_Valheim-5.4.2351
treextr-PathOfValheiman-4.13.10
Meldurson-LetMeTameYou-0.1.0
```

Run:

```powershell
./scripts/package_thunderstore.ps1
```

After packaging, open the ZIP and verify that `manifest.json`, `README.md`, `icon.png` and `plugins/` are directly at the ZIP root, not inside another containing folder.

### Thunderstore category disclosure

Thunderstore currently instructs authors to select the **AI Generated** category if a significant portion of the mod was created using AI tools. This project was substantially developed with AI assistance, so select that category during upload.

Also choose the normal Valheim category/categories that best describe the package (for example a utility/compatibility category available in the uploader).

## 4. Nexus Mods

Run:

```powershell
./scripts/package_nexus.ps1
```

The produced archive contains:

```text
BepInEx/plugins/POVLMTYBeastAIAdapter/POVLMTYBeastAIAdapter.dll
README.md
```

On the Nexus mod page:

- game: Valheim
- main file: `POVLMTYBeastAIAdapter-0.3.1-Nexus.zip`
- required mods/resources: BepInExPack Valheim, PathOfValheiman, LetMeTameYou
- do not upload copies of those dependencies inside your archive
- use `NEXUS_DESCRIPTION.md` as the page description

The dependency/credit section matters because this adapter exists specifically to interoperate with two third-party mods.

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
