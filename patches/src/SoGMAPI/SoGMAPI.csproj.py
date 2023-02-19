from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        (
            "The modding API for Stardew Valley",
            "The modding API for Secrets of Grindea",
        ),
        ("x64", "x86"),
        (
            """<Reference Include="Stardew Valley" HintPath="$(GamePath)\\Stardew Valley.dll" Private="False" />""",
            """<Reference Include="SoG" HintPath="$(GamePath)\\Secrets Of Grindea.exe" Private="False" />""",
        ),
        (
            """  <ItemGroup>
    <Reference Include="..\\..\\build\\0Harmony.dll" Private="True" />
    <Reference Include="SoG" HintPath="$(GamePath)\\Secrets Of Grindea.exe" Private="False" />
    <Reference Include="StardewValley.GameData" HintPath="$(GamePath)\\StardewValley.GameData.dll" Private="False" />
    <Reference Include="BmFont" HintPath="$(GamePath)\\BmFont.dll" Private="False" />
    <Reference Include="GalaxyCSharp" HintPath="$(GamePath)\\GalaxyCSharp.dll" Private="False" />
    <Reference Include="Lidgren.Network" HintPath="$(GamePath)\\Lidgren.Network.dll" Private="False" />
    <Reference Include="MonoGame.Framework" HintPath="$(GamePath)\\MonoGame.Framework.dll" Private="False" />
    <Reference Include="SkiaSharp" HintPath="$(GamePath)\\SkiaSharp.dll" Private="False" />
    <Reference Include="xTile" HintPath="$(GamePath)\\xTile.dll" Private="False" />
  </ItemGroup>""",
            """  <ItemGroup>
    <Reference Include="..\\..\\build\\0Harmony.dll" Private="True" />
    <Reference Include="Lidgren.Network" HintPath="$(GamePath)\\Lidgren.Network.dll" Private="False" />
    <Reference Include="SoG" HintPath="$(GamePath)\\Secrets Of Grindea.exe" Private="False" />
    <Reference Include="..\\..\\build\\MonoGame.Framework.dll" Private="False" />
  </ItemGroup>""",
        ),
        (
            """<Content Include="..\\SoGMAPI.Web\\wwwroot\\SoGMAPI.metadata.json" Link="SoGMAPI.metadata.json" CopyToOutputDirectory="PreserveNewest" />""",
            "",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
