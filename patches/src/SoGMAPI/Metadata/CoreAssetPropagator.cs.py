from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        (
            "using SoGModdingAPI.Toolkit.Utilities;",
            """using SoGModdingAPI.Toolkit.Utilities;
using SoGModdingAPI.Framework;""",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
