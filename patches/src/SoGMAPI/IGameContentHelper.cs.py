from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        (
            "using SoGModdingAPI.Events;",
            """using SoGModdingAPI.Events;
using SoGModdingAPI.Framework;""",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
