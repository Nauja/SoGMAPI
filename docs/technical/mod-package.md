&larr; [SoGMAPI](../README.md)

The **mod build package** is an open-source NuGet package which automates the MSBuild configuration
for SoGMAPI mods and related tools. The package is fully compatible with Linux, macOS, and Windows.

## Contents
* [Use](#use)
* [Features](#features)
* [Configure](#configure)
* [Code warnings](#code-warnings)
* [FAQs](#faqs)
  * [How do I set the game path?](#custom-game-path)
  * [How do I change which files are included in the mod deploy/zip?](#how-do-i-change-which-files-are-included-in-the-mod-deployzip)
  * [Can I use the package for non-mod projects?](#can-i-use-the-package-for-non-mod-projects)
* [For SoGMAPI developers](#for-sogmapi-developers)
* [Release notes](#release-notes)

## Use
1. Create an empty library project.
2. Reference the [`Pathoschild.Stardew.ModBuildConfig` NuGet package](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig).
3. [Write your code](https://stardewvalleywiki.com/Modding:Creating_a_SMAPI_mod).
4. Compile on any platform.
5. Run the game to play with your mod.

## Features
The package includes several features to simplify mod development (see [_configure_](#configure) to
change how these work):

* **Detect game path:**  
  The package automatically finds your game folder by scanning the default install paths and
  Windows registry. It adds two MSBuild properties for use in your `.csproj` file if needed:
  `$(GamePath)` and `$(GameExecutableName)`.

* **Add assembly references:**  
  The package adds assembly references to SoGMAPI, Secrets of Grindea, and the game framework
  (MonoGame on Linux/macOS, XNA Framework on Windows). It automatically adjusts depending on which OS
  you're compiling it on. If you use [Harmony](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony),
  it can optionally add a reference to that too.

* **Copy files into the `Mods` folder:**  
  The package automatically copies your mod's DLL and PDB files, `manifest.json`, [`i18n`
  files](https://stardewvalleywiki.com/Modding:Translations) (if any), the `assets` folder (if
  any), and [build output](https://stackoverflow.com/a/10828462/262123) into your game's `Mods`
  folder when you rebuild the code, with a subfolder matching the mod's project name. That lets you
  try the mod in-game right after building it.

* **Create release zip:**  
  The package adds a zip file in your project's `bin` folder when you rebuild the code, in the
  format recommended for uploading to mod sites like Nexus Mods. This includes the same files as
  the previous feature.

* **Launch or debug mod:**  
  On Windows only, the package configures Visual Studio so you can launch the game and attach a
  debugger using _Debug > Start Debugging_ or _Debug > Start Without Debugging_. This lets you [set
  breakpoints](https://docs.microsoft.com/en-us/visualstudio/debugger/using-breakpoints?view=vs-2019)
  in your code while the game is running, or [make simple changes to the mod code without needing to
  restart the game](https://docs.microsoft.com/en-us/visualstudio/debugger/edit-and-continue?view=vs-2019).
  This is disabled on Linux/macOS due to limitations with the Mono wrapper.

* **Preconfigure common settings:**  
  The package automatically enables `.pdb` files (so error logs show line numbers to simplify
  debugging), and enables support for the simplified SDK-style `.csproj` format.

* **Add code warnings:**  
  The package runs code analysis on your mod and raises warnings for some common errors or
  pitfalls. See [_code warnings_](#code-warnings) for more info.

## Configure
### How to set options
You can configure the package by setting build properties, which are essentially tags like this:
```xml
<PropertyGroup>
    <ModFolderName>CustomModName</ModFolderName>
    <EnableModDeploy>false</EnableModDeploy>
</PropertyGroup>
```

There are two places you can put them:

* **Global properties** apply to every mod project you open on your computer. That's recommended
  for properties you want to set for all mods (e.g. a custom game path). Here's where to put them:

  1. Open the home folder on your computer (see instructions for
     [Linux](https://superuser.com/questions/409218/where-is-my-users-home-folder-in-ubuntu),
     [macOS](https://www.cnet.com/how-to/how-to-find-your-macs-home-folder-and-add-it-to-finder/),
     or [Windows](https://www.computerhope.com/issues/ch000109.htm)).
  2. Create a `secretsofgrindea.targets` file with this content:
     ```xml
     <Project>
        <PropertyGroup>
        </PropertyGroup>
     </Project>
     ```
  3. Add the properties between the `<PropertyGroup>` and `</PropertyGroup>`.

* **Project properties** apply to a specific project. This is mainly useful for mod-specific
  options like the mod name. Here's where to put them:

  1. Open the folder containing your mod's source code.
  2. Open the `.csproj` file in a text editor (Notepad is fine).
  3. Add the properties between the first `<PropertyGroup>` and `</PropertyGroup>` tags you find.

### Available properties
These are the options you can set:

<ul>
<li>Game properties:
<table>
<tr>
  <th>property</th>
  <th>effect</th>
</tr>
<tr>
<td><code>GamePath</code></td>
<td>

The absolute path to the Secrets of Grindea folder. This is auto-detected, so you usually don't need to
change it.

</td>
</tr>
<tr>
<td><code>GameModsPath</code></td>
<td>

The absolute path to the folder containing the game's installed mods (defaults to
`$(GamePath)/Mods`), used when deploying the mod files.

</td>
</tr>
<tr>
<td><code>GameExecutableName</code></td>
<td>

The filename for the game's executable (i.e. `SecretsOfGrindea.exe` on Linux/macOS or
`Secrets of Grindea.exe` on Windows). This is auto-detected, and you should almost never change this.

</td>
</tr>
<tr>
<td><code>GameFramework</code></td>
<td>

The game framework for which the mod is being compiled (one of `Xna` or `MonoGame`). This is
auto-detected based on the platform, and you should almost never change this.

</td>
</tr>
</table>
</li>

<li>Mod build properties:
<table>
<tr>
  <th>property</th>
  <th>effect</th>
</tr>
<tr>
<td><code>EnableHarmony</code></td>
<td>

Whether to add a reference to [Harmony](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony)
(default `false`). This is only needed if you use Harmony.

</td>
</tr>
<tr>
<td><code>EnableModDeploy</code></td>
<td>

Whether to copy the mod files into your game's `Mods` folder (default `true`).

</td>
</tr>
<tr>
<td><code>EnableModZip</code></td>
<td>

Whether to create a release-ready `.zip` file in the mod project's `bin` folder (default `true`).

</td>
</tr>
<tr>
<td><code>ModFolderName</code></td>
<td>

The mod name for its folder under `Mods` and its release zip (defaults to the project name).

</td>
</tr>
<tr>
<td><code>ModZipPath</code></td>
<td>

The folder path where the release zip is created (defaults to the project's `bin` folder).

</td>
</tr>
</table>
</li>

<li>Specialized properties:
<table>
<tr>
  <th>property</th>
  <th>effect</th>
</tr>
<tr>
<td><code>CopyModReferencesToBuildOutput</code></td>
<td>

Whether to copy game and framework DLLs into the mod folder (default `false`). This is useful for
unit test projects, but not needed for mods that'll be run through SoGMAPI.

</td>
</tr>
<tr>
<td><code>EnableGameDebugging</code></td>
<td>

Whether to configure the project so you can launch or debug the game through the _Debug_ menu in
Visual Studio (default `true`). There's usually no reason to change this, unless it's a unit test
project.

</td>
</tr>
<tr>
<td><code>IgnoreModFilePatterns</code></td>
<td>

A comma-delimited list of regex patterns matching files to ignore when deploying or zipping the mod
files (default empty). For crossplatform compatibility, you should replace path delimiters with `[/\\]`.

For example, this excludes all `.txt` and `.pdf` files, as well as the `assets/paths.png` file:

```xml
<IgnoreModFilePatterns>\.txt$, \.pdf$, assets[/\\]paths.png</IgnoreModFilePatterns>
```

</td>
</tr>
</table>
</li>
</ul>

## Code warnings
### Overview
The NuGet package adds code warnings in Visual Studio specific to Secrets of Grindea. For example:  
![](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/screenshots/code-analyzer-example.png?raw=true)

You can [hide the warnings](https://visualstudiomagazine.com/articles/2017/09/01/hide-compiler-warnings.aspx)
if needed using the warning ID (shown under 'code' in the Error List).

See below for help with specific warnings.

### Avoid implicit net field cast
Warning text:
> This implicitly converts '{{expression}}' from {{net type}} to {{other type}}, but
> {{net type}} has unintuitive implicit conversion rules. Consider comparing against the actual
> value instead to avoid bugs.

Secrets of Grindea uses net types (like `NetBool` and `NetInt`) to handle multiplayer sync. These types
can implicitly convert to their equivalent normal values (like `bool x = new NetBool()`), but their
conversion rules are unintuitive and error-prone. For example,
`item?.category == null && item?.category != null` can both be true at once, and
`building.indoors != null` can be true for a null value.

Suggested fix:
* Some net fields have an equivalent non-net property like `monster.Health` (`int`) instead of
  `monster.health` (`NetInt`). The package will add a separate [AvoidNetField](#avoid-net-field) warning for
  these. Use the suggested property instead.
* For a reference type (i.e. one that can contain `null`), you can use the `.Value` property:
  ```c#
  if (building.indoors.Value == null)
  ```
  Or convert the value before comparison:
  ```c#
  GameLocation indoors = building.indoors;
  if(indoors == null)
     // ...
  ```
* For a value type (i.e. one that can't contain `null`), check if the object is null (if applicable)
  and compare with `.Value`:
  ```cs
  if (item != null && item.category.Value == 0)
  ```

### Avoid net field
Warning text:
> '{{expression}}' is a {{net type}} field; consider using the {{property name}} property instead.

Your code accesses a net field, which has some unusual behavior (see [AvoidImplicitNetFieldCast](#avoid-implicit-net-field-cast)).
This field has an equivalent non-net property that avoids those issues.

Suggested fix: access the suggested property name instead.

### Avoid obsolete field
Warning text:
> The '{{old field}}' field is obsolete and should be replaced with '{{new field}}'.

Your code accesses a field which is obsolete or no longer works. Use the suggested field instead.

### Wrong processor architecture
Warning text:
> The target platform should be set to 'Any CPU' for compatibility with both 32-bit and 64-bit
> versions of Secrets of Grindea (currently set to '{{current platform}}').

Mods can be used in either 32-bit or 64-bit mode. Your project's target platform isn't set to the
default 'Any CPU', so it won't work in both. You can fix it by [setting the target platform to
'Any CPU'](https://docs.microsoft.com/en-ca/visualstudio/ide/how-to-configure-projects-to-target-platforms).

## FAQs
### How do I set the game path?<span id="custom-game-path"></span>
The package detects where your game is installed automatically, so you usually don't need to set it
manually. If it can't find your game or you have multiple installs, you can specify the path
yourself.

To do that:

1. Get the full folder path containing the Secrets of Grindea executable.
2. See [_configure_](#configure) to add this property:
   ```xml
   <PropertyGroup>
       <GamePath>PATH_HERE</GamePath>
   </PropertyGroup>
   ```
3. Replace `PATH_HERE` with your game's folder path (don't add quotes).

The configuration will check your custom path first, then fall back to the default paths (so it'll
still compile on a different computer).

### How do I change which files are included in the mod deploy/zip?
For custom files, you can [add/remove them in the build output](https://stackoverflow.com/a/10828462/262123).
(If your project references another mod, make sure the reference is [_not_ marked 'copy
local'](https://msdn.microsoft.com/en-us/library/t1zz5y8c(v=vs.100).aspx).)

To exclude a file the package copies by default, see `IgnoreModFilePatterns` under
[_configure_](#configure).

### Can I use the package for non-mod projects?
You can use the package in non-mod projects too (e.g. unit tests or framework DLLs). Just disable
the mod-related package features (see [_configure_](#configure)):

```xml
<EnableGameDebugging>false</EnableGameDebugging>
<EnableModDeploy>false</EnableModDeploy>
<EnableModZip>false</EnableModZip>
```

If you need to copy the referenced DLLs into your build output, add this too:
```xml
<CopyModReferencesToBuildOutput>true</CopyModReferencesToBuildOutput>
```

## For SoGMAPI developers
The mod build package consists of three projects:

project                                           | purpose
------------------------------------------------- | ----------------
`SoGModdingAPI.ModBuildConfig`                | Configures the build (references, deploying the mod files, setting up debugging, etc).
`SoGModdingAPI.ModBuildConfig.Analyzer`       | Adds C# analyzers which show code warnings in Visual Studio.
`SoGModdingAPI.ModBuildConfig.Analyzer.Tests` | Unit tests for the C# analyzers.

The NuGet package is generated automatically in `SoGModdingAPI.ModBuildConfig`'s `bin` folder
when you compile it.

## Release notes
### 0.1.1
Released 23 June 2021.

* Initial release.
* Forked from SMAPI
* Can launch game
* Can do simple console commands