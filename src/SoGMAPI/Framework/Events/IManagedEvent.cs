namespace SoGModdingAPI.Framework.Events
{
    /// <summary>Metadata for an event raised by SoGMAPI.</summary>
    internal interface IManagedEvent
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A human-readable name for the event.</summary>
        string EventName { get; }

        /// <summary>Whether any handlers are listening to the event.</summary>
        bool HasListeners { get; }
    }
}
