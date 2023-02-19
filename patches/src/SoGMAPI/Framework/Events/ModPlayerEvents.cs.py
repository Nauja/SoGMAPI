from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        (
            """        /// <inheritdoc />
        public event EventHandler<InventoryChangedEventArgs> InventoryChanged
        {
            add => this.EventManager.InventoryChanged.Add(value, this.Mod);
            remove => this.EventManager.InventoryChanged.Remove(value);
        }

        /// <inheritdoc />
        public event EventHandler<LevelChangedEventArgs> LevelChanged
        {
            add => this.EventManager.LevelChanged.Add(value, this.Mod);
            remove => this.EventManager.LevelChanged.Remove(value);
        }

        /// <inheritdoc />
        public event EventHandler<WarpedEventArgs> Warped
        {
            add => this.EventManager.Warped.Add(value, this.Mod);
            remove => this.EventManager.Warped.Remove(value);
        }""",
            "",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
