from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        (
            """  <ItemGroup>
    <Reference Include="Stardew Valley" HintPath="$(GamePath)\\Stardew Valley.dll" Private="False" />
  </ItemGroup>""",
            """  <ItemGroup>
    <Reference Include="SoG" HintPath="$(GamePath)\\Secrets Of Grindea.exe" Private="False" />
  </ItemGroup>""",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
