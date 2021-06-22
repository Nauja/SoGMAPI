using System;

namespace SoGModdingAPI.Events
{
    /// <summary>Events serving specialized edge cases that shouldn't be used by most mods.</summary>
    public interface ISpecializedEvents
    {
        /// <summary>Raised before the game state is updated (≈60 times per second), regardless of normal SMAPI validation. This event is not thread-safe and may be invoked while game logic is running asynchronously. Changes to game state in this method may crash the game or corrupt an in-progress save. Do not use this event unless you're fully aware of the context in which your code will be run. Mods using this event will trigger a stability warning in the SMAPI console.</summary>
        event EventHandler<UnvalidatedUpdateTickingEventArgs> UnvalidatedUpdateTicking;

        /// <summary>Raised after the game state is updated (≈60 times per second), regardless of normal SMAPI validation. This event is not thread-safe and may be invoked while game logic is running asynchronously. Changes to game state in this method may crash the game or corrupt an in-progress save. Do not use this event unless you're fully aware of the context in which your code will be run. Mods using this event will trigger a stability warning in the SMAPI console.</summary>
        event EventHandler<UnvalidatedUpdateTickedEventArgs> UnvalidatedUpdateTicked;
    }
}
