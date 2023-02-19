from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        ("using SoG.Menus;", ""),
        ("IsSaveLoaded => ", "IsSaveLoaded => false; //"),
        ("IsSaving => ", "IsSaving => false; //"),
        ("IsPlayerFree => ", "IsPlayerFree => true; //"),
        ("CanPlayerMove => ", "CanPlayerMove => true; //"),
        ("ScreenId => ", "ScreenId => 0; //"),
        ("IsMultiplayer => ", "IsMultiplayer => false; //"),
        ("IsSplitScreen => ", "IsSplitScreen => false; //"),
        ("HasRemotePlayers => ", "HasRemotePlayers => false; //"),
        ("IsMainPlayer => ", "IsMainPlayer => true; //"),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
