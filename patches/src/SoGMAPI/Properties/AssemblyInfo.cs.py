from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    actions.add(
        ReplaceText(
            "",
            """[assembly: InternalsVisibleTo("SoGMAPI.Tests")]""",
            "",
        )
    )

    return actions
