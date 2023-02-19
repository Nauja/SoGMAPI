from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        (
            """        /// <summary>Raised after items are added or removed to a player's inventory. NOTE: this event is currently only raised for the current player.</summary>
        event EventHandler<InventoryChangedEventArgs> InventoryChanged;

        /// <summary>Raised after a player skill level changes. This happens as soon as they level up, not when the game notifies the player after their character goes to bed.  NOTE: this event is currently only raised for the current player.</summary>
        event EventHandler<LevelChangedEventArgs> LevelChanged;

        /// <summary>Raised after a player warps to a new location. NOTE: this event is currently only raised for the current player.</summary>
        event EventHandler<WarpedEventArgs> Warped;""",
            "",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
