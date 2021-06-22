using System;
using SoGModdingAPI.Events;

namespace SoGModdingAPI.Framework.Events
{
    /// <summary>Events linked to the game's update loop. The update loop runs roughly ≈60 times/second to run game logic like state changes, action handling, etc. These can be useful, but you should consider more semantic events like <see cref="IInputEvents"/> if possible.</summary>
    internal class ModGameLoopEvents : ModEventsBase, IGameLoopEvents
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Raised after the game is launched, right before the first update tick.</summary>
        public event EventHandler<GameLaunchedEventArgs> GameLaunched
        {
            add => this.EventManager.GameLaunched.Add(value, this.Mod);
            remove => this.EventManager.GameLaunched.Remove(value);
        }

        /// <summary>Raised before the game performs its overall update tick (≈60 times per second).</summary>
        public event EventHandler<UpdateTickingEventArgs> UpdateTicking
        {
            add => this.EventManager.UpdateTicking.Add(value, this.Mod);
            remove => this.EventManager.UpdateTicking.Remove(value);
        }

        /// <summary>Raised after the game performs its overall update tick (≈60 times per second).</summary>
        public event EventHandler<UpdateTickedEventArgs> UpdateTicked
        {
            add => this.EventManager.UpdateTicked.Add(value, this.Mod);
            remove => this.EventManager.UpdateTicked.Remove(value);
        }

        /// <summary>Raised once per second before the game state is updated.</summary>
        public event EventHandler<OneSecondUpdateTickingEventArgs> OneSecondUpdateTicking
        {
            add => this.EventManager.OneSecondUpdateTicking.Add(value, this.Mod);
            remove => this.EventManager.OneSecondUpdateTicking.Remove(value);
        }

        /// <summary>Raised once per second after the game state is updated.</summary>
        public event EventHandler<OneSecondUpdateTickedEventArgs> OneSecondUpdateTicked
        {
            add => this.EventManager.OneSecondUpdateTicked.Add(value, this.Mod);
            remove => this.EventManager.OneSecondUpdateTicked.Remove(value);
        }

        /// <summary>Raised after the game returns to the title screen.</summary>
        public event EventHandler<ReturnedToTitleEventArgs> ReturnedToTitle
        {
            add => this.EventManager.ReturnedToTitle.Add(value, this.Mod);
            remove => this.EventManager.ReturnedToTitle.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod which uses this instance.</param>
        /// <param name="eventManager">The underlying event manager.</param>
        internal ModGameLoopEvents(IModMetadata mod, EventManager eventManager)
            : base(mod, eventManager) { }
    }
}
