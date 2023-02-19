using SoGModdingAPI.Events;

namespace SoGModdingAPI.Framework.Events
{
    /// <summary>Manages SoGMAPI events.</summary>
    internal class EventManager
    {
        /*********
        ** Events
        *********/
        /****
        ** Content
        ****/
        /// <inheritdoc cref="IContentEvents.AssetRequested" />
        public readonly ManagedEvent<AssetRequestedEventArgs> AssetRequested;

        /// <inheritdoc cref="IContentEvents.AssetsInvalidated" />
        public readonly ManagedEvent<AssetsInvalidatedEventArgs> AssetsInvalidated;

        /// <inheritdoc cref="IContentEvents.AssetReady" />
        public readonly ManagedEvent<AssetReadyEventArgs> AssetReady;




        /****
        ** Display
        ****/


        /// <inheritdoc cref="IDisplayEvents.Rendering" />
        public readonly ManagedEvent<RenderingEventArgs> Rendering;

        /// <inheritdoc cref="IDisplayEvents.Rendered" />
        public readonly ManagedEvent<RenderedEventArgs> Rendered;

        /// <inheritdoc cref="IDisplayEvents.RenderingWorld" />
        public readonly ManagedEvent<RenderingWorldEventArgs> RenderingWorld;

        /// <inheritdoc cref="IDisplayEvents.RenderedWorld" />
        public readonly ManagedEvent<RenderedWorldEventArgs> RenderedWorld;

        /// <inheritdoc cref="IDisplayEvents.RenderingActiveMenu" />
        public readonly ManagedEvent<RenderingActiveMenuEventArgs> RenderingActiveMenu;

        /// <inheritdoc cref="IDisplayEvents.RenderedActiveMenu" />
        public readonly ManagedEvent<RenderedActiveMenuEventArgs> RenderedActiveMenu;

        /// <inheritdoc cref="IDisplayEvents.RenderingHud" />
        public readonly ManagedEvent<RenderingHudEventArgs> RenderingHud;

        /// <inheritdoc cref="IDisplayEvents.RenderedHud" />
        public readonly ManagedEvent<RenderedHudEventArgs> RenderedHud;

        /// <inheritdoc cref="IDisplayEvents.WindowResized" />
        public readonly ManagedEvent<WindowResizedEventArgs> WindowResized;

        /****
        ** Game loop
        ****/
        /// <inheritdoc cref="IGameLoopEvents.GameLaunched" />
        public readonly ManagedEvent<GameLaunchedEventArgs> GameLaunched;

        /// <inheritdoc cref="IGameLoopEvents.UpdateTicking" />
        public readonly ManagedEvent<UpdateTickingEventArgs> UpdateTicking;

        /// <inheritdoc cref="IGameLoopEvents.UpdateTicked" />
        public readonly ManagedEvent<UpdateTickedEventArgs> UpdateTicked;

        /// <inheritdoc cref="IGameLoopEvents.OneSecondUpdateTicking" />
        public readonly ManagedEvent<OneSecondUpdateTickingEventArgs> OneSecondUpdateTicking;

        /// <inheritdoc cref="IGameLoopEvents.OneSecondUpdateTicked" />
        public readonly ManagedEvent<OneSecondUpdateTickedEventArgs> OneSecondUpdateTicked;

        /// <inheritdoc cref="IGameLoopEvents.SaveCreating" />
        public readonly ManagedEvent<SaveCreatingEventArgs> SaveCreating;

        /// <inheritdoc cref="IGameLoopEvents.SaveCreated" />
        public readonly ManagedEvent<SaveCreatedEventArgs> SaveCreated;

        /// <inheritdoc cref="IGameLoopEvents.Saving" />
        public readonly ManagedEvent<SavingEventArgs> Saving;

        /// <inheritdoc cref="IGameLoopEvents.Saved" />
        public readonly ManagedEvent<SavedEventArgs> Saved;

        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded" />
        public readonly ManagedEvent<SaveLoadedEventArgs> SaveLoaded;

        /// <inheritdoc cref="IGameLoopEvents.DayStarted" />
        public readonly ManagedEvent<DayStartedEventArgs> DayStarted;

        /// <inheritdoc cref="IGameLoopEvents.DayEnding" />
        public readonly ManagedEvent<DayEndingEventArgs> DayEnding;

        /// <inheritdoc cref="IGameLoopEvents.TimeChanged" />
        public readonly ManagedEvent<TimeChangedEventArgs> TimeChanged;

        /// <inheritdoc cref="IGameLoopEvents.ReturnedToTitle" />
        public readonly ManagedEvent<ReturnedToTitleEventArgs> ReturnedToTitle;

        /****
        ** Input
        ****/
        /// <inheritdoc cref="IInputEvents.ButtonsChanged" />
        public readonly ManagedEvent<ButtonsChangedEventArgs> ButtonsChanged;

        /// <inheritdoc cref="IInputEvents.ButtonPressed" />
        public readonly ManagedEvent<ButtonPressedEventArgs> ButtonPressed;

        /// <inheritdoc cref="IInputEvents.ButtonReleased" />
        public readonly ManagedEvent<ButtonReleasedEventArgs> ButtonReleased;

        /// <inheritdoc cref="IInputEvents.CursorMoved" />
        public readonly ManagedEvent<CursorMovedEventArgs> CursorMoved;

        /// <inheritdoc cref="IInputEvents.MouseWheelScrolled" />
        public readonly ManagedEvent<MouseWheelScrolledEventArgs> MouseWheelScrolled;

        /****
        ** Multiplayer
        ****/
        /// <inheritdoc cref="IMultiplayerEvents.PeerContextReceived" />
        public readonly ManagedEvent<PeerContextReceivedEventArgs> PeerContextReceived;

        /// <inheritdoc cref="IMultiplayerEvents.PeerConnected" />
        public readonly ManagedEvent<PeerConnectedEventArgs> PeerConnected;

        /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived" />
        public readonly ManagedEvent<ModMessageReceivedEventArgs> ModMessageReceived;

        /// <inheritdoc cref="IMultiplayerEvents.PeerDisconnected" />
        public readonly ManagedEvent<PeerDisconnectedEventArgs> PeerDisconnected;



        /// <inheritdoc cref="ISpecializedEvents.UnvalidatedUpdateTicking" />
        public readonly ManagedEvent<UnvalidatedUpdateTickingEventArgs> UnvalidatedUpdateTicking;

        /// <inheritdoc cref="ISpecializedEvents.UnvalidatedUpdateTicked" />
        public readonly ManagedEvent<UnvalidatedUpdateTickedEventArgs> UnvalidatedUpdateTicked;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modRegistry">The mod registry with which to identify mods.</param>
        public EventManager(ModRegistry modRegistry)
        {
            // create shortcut initializers
            ManagedEvent<TEventArgs> ManageEventOf<TEventArgs>(string typeName, string eventName)
            {
                return new ManagedEvent<TEventArgs>($"{typeName}.{eventName}", modRegistry);
            }

            // init events
            this.AssetRequested = ManageEventOf<AssetRequestedEventArgs>(nameof(IModEvents.Content), nameof(IContentEvents.AssetRequested));
            this.AssetsInvalidated = ManageEventOf<AssetsInvalidatedEventArgs>(nameof(IModEvents.Content), nameof(IContentEvents.AssetsInvalidated));
            this.AssetReady = ManageEventOf<AssetReadyEventArgs>(nameof(IModEvents.Content), nameof(IContentEvents.AssetReady));

            this.Rendering = ManageEventOf<RenderingEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.Rendering));
            this.Rendered = ManageEventOf<RenderedEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.Rendered));
            this.RenderingWorld = ManageEventOf<RenderingWorldEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderingWorld));
            this.RenderedWorld = ManageEventOf<RenderedWorldEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderedWorld));
            this.RenderingActiveMenu = ManageEventOf<RenderingActiveMenuEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderingActiveMenu));
            this.RenderedActiveMenu = ManageEventOf<RenderedActiveMenuEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderedActiveMenu));
            this.RenderingHud = ManageEventOf<RenderingHudEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderingHud));
            this.RenderedHud = ManageEventOf<RenderedHudEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderedHud));
            this.WindowResized = ManageEventOf<WindowResizedEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.WindowResized));

            this.GameLaunched = ManageEventOf<GameLaunchedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.GameLaunched));
            this.UpdateTicking = ManageEventOf<UpdateTickingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.UpdateTicking));
            this.UpdateTicked = ManageEventOf<UpdateTickedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.UpdateTicked));
            this.OneSecondUpdateTicking = ManageEventOf<OneSecondUpdateTickingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.OneSecondUpdateTicking));
            this.OneSecondUpdateTicked = ManageEventOf<OneSecondUpdateTickedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.OneSecondUpdateTicked));
            this.SaveCreating = ManageEventOf<SaveCreatingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.SaveCreating));
            this.SaveCreated = ManageEventOf<SaveCreatedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.SaveCreated));
            this.Saving = ManageEventOf<SavingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.Saving));
            this.Saved = ManageEventOf<SavedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.Saved));
            this.SaveLoaded = ManageEventOf<SaveLoadedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.SaveLoaded));
            this.DayStarted = ManageEventOf<DayStartedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.DayStarted));
            this.DayEnding = ManageEventOf<DayEndingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.DayEnding));
            this.TimeChanged = ManageEventOf<TimeChangedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.TimeChanged));
            this.ReturnedToTitle = ManageEventOf<ReturnedToTitleEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.ReturnedToTitle));

            this.ButtonsChanged = ManageEventOf<ButtonsChangedEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.ButtonsChanged));
            this.ButtonPressed = ManageEventOf<ButtonPressedEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.ButtonPressed));
            this.ButtonReleased = ManageEventOf<ButtonReleasedEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.ButtonReleased));
            this.CursorMoved = ManageEventOf<CursorMovedEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.CursorMoved));
            this.MouseWheelScrolled = ManageEventOf<MouseWheelScrolledEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.MouseWheelScrolled));

            this.PeerContextReceived = ManageEventOf<PeerContextReceivedEventArgs>(nameof(IModEvents.Multiplayer), nameof(IMultiplayerEvents.PeerContextReceived));
            this.PeerConnected = ManageEventOf<PeerConnectedEventArgs>(nameof(IModEvents.Multiplayer), nameof(IMultiplayerEvents.PeerConnected));
            this.ModMessageReceived = ManageEventOf<ModMessageReceivedEventArgs>(nameof(IModEvents.Multiplayer), nameof(IMultiplayerEvents.ModMessageReceived));
            this.PeerDisconnected = ManageEventOf<PeerDisconnectedEventArgs>(nameof(IModEvents.Multiplayer), nameof(IMultiplayerEvents.PeerDisconnected));


            this.UnvalidatedUpdateTicking = ManageEventOf<UnvalidatedUpdateTickingEventArgs>(nameof(IModEvents.Specialized), nameof(ISpecializedEvents.UnvalidatedUpdateTicking));
            this.UnvalidatedUpdateTicked = ManageEventOf<UnvalidatedUpdateTickedEventArgs>(nameof(IModEvents.Specialized), nameof(ISpecializedEvents.UnvalidatedUpdateTicked));
        }
    }
}
