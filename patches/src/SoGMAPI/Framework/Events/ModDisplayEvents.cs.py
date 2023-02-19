from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        (
            """        /// <inheritdoc />
        public event EventHandler<MenuChangedEventArgs> MenuChanged
        {
            add => this.EventManager.MenuChanged.Add(value, this.Mod);
            remove => this.EventManager.MenuChanged.Remove(value);
        }""",
            "",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
