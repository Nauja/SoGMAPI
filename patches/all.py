from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList:
    """Global fixes."""
    actions = ActionList()

    for a, b in (
        ('[assembly: InternalsVisibleTo("SoGMAPI.Tests")]', ""),
        ('[assembly: InternalsVisibleTo("SoGMAPI.Web")]', ""),
        ("413150", "269770"),
        ("x64", "x86"),
        ("SMAPI_", "SOGMAPI_"),
        ("using SMAPI", "using SoGMAPI"),
        ("namespace SMAPI", "namespace SoGMAPI"),
        ("using StardewValley", "using SoG"),
        ("namespace StardewValley", "namespace SoG"),
        ("common/Stardew Valley", "common/SecretsOfGrindea"),
        ("common\\Stardew Valley", "common\\SecretsOfGrindea"),
        ("Stardew Valley/game", "SecretsOfGrindea/game"),
        ("Games\\Stardew Valley", "Games\\SecretsOfGrindea"),
        (
            "ModifiableWindowsApps\\Stardew Valley",
            "ModifiableWindowsApps\\SecretsOfGrindea",
        ),
        ("StardewModdingAPI", "SoGModdingAPI"),
        ("smapi-internal", "sogmapi-internal"),
        ("-smapi", "-sogmapi"),
        ("$smapiBin", "$sogmapiBin"),
        ("smapiVersion", "sogmapiVersion"),
        ("smapi/mod-data", "sogmapi/mod-data"),
        ('".smapi"', '".sogmapi"'),
        ("smapi.targets", "sogmapi.targets"),
        ("stardewvalley.targets", "secretsofgrindea.targets"),
        ("Stardew Valley.exe", "Secrets Of Grindea.exe"),
        ("StardewValley.Game1", "SoG.Game1"),
        (
            'GameAssemblyName { get; } = "Stardew Valley"',
            'GameAssemblyName { get; } = "SoG"',
        ),
        (
            """resolver.AddWithExplicitNames(AssemblyDefinition.ReadAssembly(typeof(Game1).Assembly.Location), "StardewValley", "Stardew Valley", "Netcode");""",
            """resolver.AddWithExplicitNames(AssemblyDefinition.ReadAssembly(typeof(Game1).Assembly.Location), "SoG");""",
        ),
        (
            """if (!executable.Exists)
                executable = new(Path.Combine(dir.FullName, "StardewValley.exe")); // pre-1.5.5 Linux/macOS executable
            """,
            "",
        ),
        ("#if SOGMAPI_FOR_WINDOWS", "#if true"),
        (
            "using xTile;",
            "",
        ),
        (
            "using xTile.Format;",
            "",
        ),
        (
            "using xTile.Tiles;",
            "",
        ),
        ("using xTile.Display;", ""),
        ("using BmFont;", ""),
        ("= StardewValley.", "= SoG."),
        ("using Galaxy.Api;", ""),
        ("InputButton", "Buttons"),
        ("using SoG.GameData;", ""),
        #
        (
            """// apparently valid
            if (dir.EnumerateFiles("Stardew Valley.dll").Any())
                return GameFolderType.Valid;""",
            "",
        ),
        (
            """               case Platform.Linux:
                case Platform.Mac:
                    {
                        string home = Environment.GetEnvironmentVariable("HOME")!;

                        // Linux
                        yield return $"{home}/GOG Games/SecretsOfGrindea/game";
                        yield return Directory.Exists($"{home}/.steam/steam/steamapps/common/SecretsOfGrindea")
                            ? $"{home}/.steam/steam/steamapps/common/SecretsOfGrindea"
                            : $"{home}/.local/share/Steam/steamapps/common/SecretsOfGrindea";

                        // macOS
                        yield return "/Applications/Stardew Valley.app/Contents/MacOS";
                        yield return $"{home}/Library/Application Support/Steam/steamapps/common/SecretsOfGrindea/Contents/MacOS";
                    }
                    break;
""",
            "",
        ),
        (
            '<Copy SourceFiles="$(GamePath)\Stardew Valley.deps.json" DestinationFiles="$(GamePath)\SoGModdingAPI.deps.json" Condition="!Exists(\'$(GamePath)\SoGModdingAPI.deps.json\')" />',
            "",
        ),
        (
            """<When Condition="$(OS) == 'Unix' OR $(OS) == 'OSX'">
      <PropertyGroup>
        <!-- Linux -->
        <GamePath Condition="!Exists('$(GamePath)')">$(HOME)/GOG Games/SecretsOfGrindea/game</GamePath>
        <GamePath Condition="!Exists('$(GamePath)')">$(HOME)/.steam/steam/steamapps/common/SecretsOfGrindea</GamePath>
        <GamePath Condition="!Exists('$(GamePath)')">$(HOME)/.local/share/Steam/steamapps/common/SecretsOfGrindea</GamePath>
        <GamePath Condition="!Exists('$(GamePath)')">$(HOME)/.var/app/com.valvesoftware.Steam/data/Steam/steamapps/common/SecretsOfGrindea</GamePath>

        <!-- macOS (may be 'Unix' or 'OSX') -->
        <GamePath Condition="!Exists('$(GamePath)')">/Applications/Stardew Valley.app/Contents/MacOS</GamePath>
        <GamePath Condition="!Exists('$(GamePath)')">$(HOME)/Library/Application Support/Steam/steamapps/common/SecretsOfGrindea/Contents/MacOS</GamePath>
      </PropertyGroup>
    </When>
    """,
            "",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    for project, renamed in SMAPI_PROJECTS.items():
        actions.add(ReplaceText("", project, renamed))

    return actions
