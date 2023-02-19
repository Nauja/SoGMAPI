from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        (
            """        /// <inheritdoc cref="IDisplayEvents.MenuChanged" />
        public readonly ManagedEvent<MenuChangedEventArgs> MenuChanged;""",
            "",
        ),
        (
            """        /// <inheritdoc cref="IContentEvents.LocaleChanged" />
        public readonly ManagedEvent<LocaleChangedEventArgs> LocaleChanged;""",
            "",
        ),
        (
            """        /****
        ** Player
        ****/
        /// <inheritdoc cref="IPlayerEvents.InventoryChanged" />
        public readonly ManagedEvent<InventoryChangedEventArgs> InventoryChanged;

        /// <inheritdoc cref="IPlayerEvents.LevelChanged" />
        public readonly ManagedEvent<LevelChangedEventArgs> LevelChanged;

        /// <inheritdoc cref="IPlayerEvents.Warped" />
        public readonly ManagedEvent<WarpedEventArgs> Warped;

        /****
        ** World
        ****/
        /// <inheritdoc cref="IWorldEvents.LocationListChanged" />
        public readonly ManagedEvent<LocationListChangedEventArgs> LocationListChanged;

        /// <inheritdoc cref="IWorldEvents.BuildingListChanged" />
        public readonly ManagedEvent<BuildingListChangedEventArgs> BuildingListChanged;

        /// <inheritdoc cref="IWorldEvents.DebrisListChanged" />
        public readonly ManagedEvent<DebrisListChangedEventArgs> DebrisListChanged;

        /// <inheritdoc cref="IWorldEvents.LargeTerrainFeatureListChanged" />
        public readonly ManagedEvent<LargeTerrainFeatureListChangedEventArgs> LargeTerrainFeatureListChanged;

        /// <inheritdoc cref="IWorldEvents.NpcListChanged" />
        public readonly ManagedEvent<NpcListChangedEventArgs> NpcListChanged;

        /// <inheritdoc cref="IWorldEvents.ObjectListChanged" />
        public readonly ManagedEvent<ObjectListChangedEventArgs> ObjectListChanged;

        /// <inheritdoc cref="IWorldEvents.ChestInventoryChanged" />
        public readonly ManagedEvent<ChestInventoryChangedEventArgs> ChestInventoryChanged;

        /// <inheritdoc cref="IWorldEvents.TerrainFeatureListChanged" />
        public readonly ManagedEvent<TerrainFeatureListChangedEventArgs> TerrainFeatureListChanged;

        /// <inheritdoc cref="IWorldEvents.FurnitureListChanged" />
        public readonly ManagedEvent<FurnitureListChangedEventArgs> FurnitureListChanged;

        /****
        ** Specialized
        ****/
        /// <inheritdoc cref="ISpecializedEvents.LoadStageChanged" />
        public readonly ManagedEvent<LoadStageChangedEventArgs> LoadStageChanged;""",
            "",
        ),
        (
            """            this.LocaleChanged = ManageEventOf<LocaleChangedEventArgs>(nameof(IModEvents.Content), nameof(IContentEvents.LocaleChanged));

            this.MenuChanged = ManageEventOf<MenuChangedEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.MenuChanged));""",
            "",
        ),
        (
            """            this.InventoryChanged = ManageEventOf<InventoryChangedEventArgs>(nameof(IModEvents.Player), nameof(IPlayerEvents.InventoryChanged));
            this.LevelChanged = ManageEventOf<LevelChangedEventArgs>(nameof(IModEvents.Player), nameof(IPlayerEvents.LevelChanged));
            this.Warped = ManageEventOf<WarpedEventArgs>(nameof(IModEvents.Player), nameof(IPlayerEvents.Warped));

            this.BuildingListChanged = ManageEventOf<BuildingListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.LocationListChanged));
            this.DebrisListChanged = ManageEventOf<DebrisListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.DebrisListChanged));
            this.LargeTerrainFeatureListChanged = ManageEventOf<LargeTerrainFeatureListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.LargeTerrainFeatureListChanged));
            this.LocationListChanged = ManageEventOf<LocationListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.BuildingListChanged));
            this.NpcListChanged = ManageEventOf<NpcListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.NpcListChanged));
            this.ObjectListChanged = ManageEventOf<ObjectListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.ObjectListChanged));
            this.ChestInventoryChanged = ManageEventOf<ChestInventoryChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.ChestInventoryChanged));
            this.TerrainFeatureListChanged = ManageEventOf<TerrainFeatureListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.TerrainFeatureListChanged));
            this.FurnitureListChanged = ManageEventOf<FurnitureListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.FurnitureListChanged));

            this.LoadStageChanged = ManageEventOf<LoadStageChangedEventArgs>(nameof(IModEvents.Specialized), nameof(ISpecializedEvents.LoadStageChanged));""",
            "",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
