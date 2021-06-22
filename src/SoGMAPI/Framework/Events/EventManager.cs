using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SoGModdingAPI.Events;

namespace SoGModdingAPI.Framework.Events
{
    /// <summary>Manages SMAPI events.</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Private fields are deliberately named to simplify organisation.")]
    internal class EventManager
    {
        /*********
        ** Events
        *********/
        /****
        ** Display
        ****/
        /// <summary>Raised before the game draws anything to the screen in a draw tick, as soon as the sprite batch is opened. The sprite batch may be closed and reopened multiple times after this event is called, but it's only raised once per draw tick. This event isn't useful for drawing to the screen, since the game will draw over it.</summary>
        public readonly ManagedEvent<RenderingEventArgs> Rendering;

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen. Since the game may open/close the sprite batch multiple times in a draw tick, the sprite batch may not contain everything being drawn and some things may already be rendered to the screen. Content drawn to the sprite batch at this point will be drawn over all vanilla content (including menus, HUD, and cursor).</summary>
        public readonly ManagedEvent<RenderedEventArgs> Rendered;

        /// <summary>Raised after the game window is resized.</summary>
        public readonly ManagedEvent<WindowResizedEventArgs> WindowResized;

        /****
        ** Game loop
        ****/
        /// <summary>Raised after the game is launched, right before the first update tick.</summary>
        public readonly ManagedEvent<GameLaunchedEventArgs> GameLaunched;

        /// <summary>Raised before the game performs its overall update tick (≈60 times per second).</summary>
        public readonly ManagedEvent<UpdateTickingEventArgs> UpdateTicking;

        /// <summary>Raised after the game performs its overall update tick (≈60 times per second).</summary>
        public readonly ManagedEvent<UpdateTickedEventArgs> UpdateTicked;

        /// <summary>Raised once per second before the game performs its overall update tick.</summary>
        public readonly ManagedEvent<OneSecondUpdateTickingEventArgs> OneSecondUpdateTicking;

        /// <summary>Raised once per second after the game performs its overall update tick.</summary>
        public readonly ManagedEvent<OneSecondUpdateTickedEventArgs> OneSecondUpdateTicked;

        /// <summary>Raised after the game returns to the title screen.</summary>
        public readonly ManagedEvent<ReturnedToTitleEventArgs> ReturnedToTitle;

        /****
        ** Input
        ****/
        /// <summary>Raised after the player presses or releases any buttons on the keyboard, controller, or mouse.</summary>
        public readonly ManagedEvent<ButtonsChangedEventArgs> ButtonsChanged;

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        public readonly ManagedEvent<ButtonPressedEventArgs> ButtonPressed;

        /// <summary>Raised after the player released a button on the keyboard, controller, or mouse.</summary>
        public readonly ManagedEvent<ButtonReleasedEventArgs> ButtonReleased;

        /// <summary>Raised after the player moves the in-game cursor.</summary>
        public readonly ManagedEvent<CursorMovedEventArgs> CursorMoved;

        /// <summary>Raised after the player scrolls the mouse wheel.</summary>
        public readonly ManagedEvent<MouseWheelScrolledEventArgs> MouseWheelScrolled;

        /****
        ** Multiplayer
        ****/
        /// <summary>Raised after a mod message is received over the network.</summary>
        public readonly ManagedEvent<ModMessageReceivedEventArgs> ModMessageReceived;

        /****
        ** Specialized
        ****/

        /// <summary>Raised before the game performs its overall update tick (≈60 times per second). See notes on <see cref="ISpecializedEvents.UnvalidatedUpdateTicking"/>.</summary>
        public readonly ManagedEvent<UnvalidatedUpdateTickingEventArgs> UnvalidatedUpdateTicking;

        /// <summary>Raised after the game performs its overall update tick (≈60 times per second). See notes on <see cref="ISpecializedEvents.UnvalidatedUpdateTicked"/>.</summary>
        public readonly ManagedEvent<UnvalidatedUpdateTickedEventArgs> UnvalidatedUpdateTicked;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modRegistry">The mod registry with which to identify mods.</param>
        public EventManager(ModRegistry modRegistry)
        {
            // create shortcut initializers
            ManagedEvent<TEventArgs> ManageEventOf<TEventArgs>(string typeName, string eventName, bool isPerformanceCritical = false)
            {
                return new ManagedEvent<TEventArgs>($"{typeName}.{eventName}", modRegistry, isPerformanceCritical);
            }

            // init events (new)
            this.Rendering = ManageEventOf<RenderingEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.Rendering), isPerformanceCritical: true);
            this.Rendered = ManageEventOf<RenderedEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.Rendered), isPerformanceCritical: true);
            this.WindowResized = ManageEventOf<WindowResizedEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.WindowResized));

            this.GameLaunched = ManageEventOf<GameLaunchedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.GameLaunched));
            this.UpdateTicking = ManageEventOf<UpdateTickingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.UpdateTicking), isPerformanceCritical: true);
            this.UpdateTicked = ManageEventOf<UpdateTickedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.UpdateTicked), isPerformanceCritical: true);
            this.OneSecondUpdateTicking = ManageEventOf<OneSecondUpdateTickingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.OneSecondUpdateTicking), isPerformanceCritical: true);
            this.OneSecondUpdateTicked = ManageEventOf<OneSecondUpdateTickedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.OneSecondUpdateTicked), isPerformanceCritical: true);
            this.ReturnedToTitle = ManageEventOf<ReturnedToTitleEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.ReturnedToTitle));

            this.ButtonsChanged = ManageEventOf<ButtonsChangedEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.ButtonsChanged));
            this.ButtonPressed = ManageEventOf<ButtonPressedEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.ButtonPressed));
            this.ButtonReleased = ManageEventOf<ButtonReleasedEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.ButtonReleased));
            this.CursorMoved = ManageEventOf<CursorMovedEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.CursorMoved), isPerformanceCritical: true);
            this.MouseWheelScrolled = ManageEventOf<MouseWheelScrolledEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.MouseWheelScrolled));

            this.ModMessageReceived = ManageEventOf<ModMessageReceivedEventArgs>(nameof(IModEvents.Multiplayer), nameof(IMultiplayerEvents.ModMessageReceived));

            this.UnvalidatedUpdateTicking = ManageEventOf<UnvalidatedUpdateTickingEventArgs>(nameof(IModEvents.Specialized), nameof(ISpecializedEvents.UnvalidatedUpdateTicking), isPerformanceCritical: true);
            this.UnvalidatedUpdateTicked = ManageEventOf<UnvalidatedUpdateTickedEventArgs>(nameof(IModEvents.Specialized), nameof(ISpecializedEvents.UnvalidatedUpdateTicked), isPerformanceCritical: true);
        }

        /// <summary>Get all managed events.</summary>
        public IEnumerable<IManagedEvent> GetAllEvents()
        {
            foreach (FieldInfo field in this.GetType().GetFields())
                yield return (IManagedEvent)field.GetValue(this);
        }
    }
}
