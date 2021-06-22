using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SoGModdingAPI.Events;
using SoGModdingAPI.Framework.Events;
using SoGModdingAPI.Framework.Networking;
using SoGModdingAPI.Framework.Reflection;
using SoGModdingAPI.Toolkit.Serialization;
using SoGModdingAPI.Utilities;

namespace SoGModdingAPI.Framework
{
    /// <summary>SMAPI's implementation of the game's core multiplayer logic.</summary>
    /// <remarks>
    /// SMAPI syncs mod context to all players through the host as such:
    ///   1. Farmhand sends ModContext + PlayerIntro.
    ///   2. If host receives ModContext: it stores the context, replies with known contexts, and forwards it to other farmhands.
    ///   3. If host receives PlayerIntro before ModContext: it stores a 'vanilla player' context, and forwards it to other farmhands.
    ///   4. If farmhand receives ModContext: it stores it.
    ///   5. If farmhand receives ServerIntro without a preceding ModContext: it stores a 'vanilla host' context.
    ///   6. If farmhand receives PlayerIntro without a preceding ModContext AND it's not the host peer: it stores a 'vanilla player' context.
    ///
    /// Once a farmhand/server stored a context, messages can be sent to that player through the SMAPI APIs.
    /// </remarks>
    internal class SMultiplayer
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>Tracks the installed mods.</summary>
        private readonly ModRegistry ModRegistry;

        /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
        private readonly JsonHelper JsonHelper;

        /// <summary>Simplifies access to private code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Manages SMAPI events.</summary>
        private readonly EventManager EventManager;

        /// <summary>A callback to invoke when a mod message is received.</summary>
        private readonly Action<ModMessageModel> OnModMessageReceived;

        /// <summary>Whether to log network traffic.</summary>
        private readonly bool LogNetworkTraffic;

        /// <summary>The backing field for <see cref="Peers"/>.</summary>
        private readonly PerScreen<IDictionary<long, MultiplayerPeer>> PeersImpl = new PerScreen<IDictionary<long, MultiplayerPeer>>(() => new Dictionary<long, MultiplayerPeer>());

        /// <summary>The backing field for <see cref="HostPeer"/>.</summary>
        private readonly PerScreen<MultiplayerPeer> HostPeerImpl = new PerScreen<MultiplayerPeer>();


        /*********
        ** Accessors
        *********/
        /// <summary>The metadata for each connected peer.</summary>
        public IDictionary<long, MultiplayerPeer> Peers => this.PeersImpl.Value;

        /// <summary>The metadata for the host player, if the current player is a farmhand.</summary>
        public MultiplayerPeer HostPeer
        {
            get => this.HostPeerImpl.Value;
            private set => this.HostPeerImpl.Value = value;
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="eventManager">Manages SMAPI events.</param>
        /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
        /// <param name="modRegistry">Tracks the installed mods.</param>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="onModMessageReceived">A callback to invoke when a mod message is received.</param>
        /// <param name="logNetworkTraffic">Whether to log network traffic.</param>
        public SMultiplayer(IMonitor monitor, EventManager eventManager, JsonHelper jsonHelper, ModRegistry modRegistry, Reflector reflection, Action<ModMessageModel> onModMessageReceived, bool logNetworkTraffic)
        {
            this.Monitor = monitor;
            this.EventManager = eventManager;
            this.JsonHelper = jsonHelper;
            this.ModRegistry = modRegistry;
            this.Reflection = reflection;
            this.OnModMessageReceived = onModMessageReceived;
            this.LogNetworkTraffic = logNetworkTraffic;
        }

        /// <summary>Perform cleanup needed when a multiplayer session ends.</summary>
        public void CleanupOnMultiplayerExit()
        {
            this.Peers.Clear();
            this.HostPeer = null;
        }

        /// <summary>A callback raised when sending a message as a farmhand.</summary>
        /// <param name="message">The message being sent.</param>
        /// <param name="sendMessage">Send an arbitrary message through the client.</param>
        /// <param name="resume">Resume sending the underlying message.</param>
        protected void OnClientSendingMessage(OutgoingMessage message, Action<OutgoingMessage> sendMessage, Action resume)
        {
            // @todo
        }

        /// <summary>Process an incoming network message as the host player.</summary>
        /// <param name="message">The message to process.</param>
        /// <param name="sendMessage">A method which sends the given message to the client.</param>
        /// <param name="resume">Process the message using the game's default logic.</param>
        public void OnServerProcessingMessage(IncomingMessage message, Action<OutgoingMessage> sendMessage, Action resume)
        {
            // @todo
        }

        /// <summary>Process an incoming network message as a farmhand.</summary>
        /// <param name="message">The message to process.</param>
        /// <param name="sendMessage">Send an arbitrary message through the client.</param>
        /// <param name="resume">Resume processing the message using the game's default logic.</param>
        /// <returns>Returns whether the message was handled.</returns>
        public void OnClientProcessingMessage(IncomingMessage message, Action<OutgoingMessage> sendMessage, Action resume)
        {
            // @todo
        }

        /// <summary>Broadcast a mod message to matching players.</summary>
        /// <param name="message">The data to send over the network.</param>
        /// <param name="messageType">A message type which receiving mods can use to decide whether it's the one they want to handle, like <c>SetPlayerLocation</c>. This doesn't need to be globally unique, since mods should check the originating mod ID.</param>
        /// <param name="fromModID">The unique ID of the mod sending the message.</param>
        /// <param name="toModIDs">The mod IDs which should receive the message on the destination computers, or <c>null</c> for all mods. Specifying mod IDs is recommended to improve performance, unless it's a general-purpose broadcast.</param>
        /// <param name="toPlayerIDs">The <see cref="Farmer.UniqueMultiplayerID" /> values for the players who should receive the message, or <c>null</c> for all players. If you don't need to broadcast to all players, specifying player IDs is recommended to reduce latency.</param>
        public void BroadcastModMessage<TMessage>(TMessage message, string messageType, string fromModID, string[] toModIDs, long[] toPlayerIDs)
        {
            // @todo
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Save a received peer.</summary>
        /// <param name="peer">The peer to add.</param>
        /// <param name="canBeHost">Whether to track the peer as the host if applicable.</param>
        /// <param name="raiseEvent">Whether to raise the <see cref="Events.EventManager.PeerContextReceived"/> event.</param>
        private void AddPeer(MultiplayerPeer peer, bool canBeHost, bool raiseEvent = true)
        {
            // @todo
        }

        /// <summary>Read the metadata context for a player.</summary>
        /// <param name="reader">The stream reader.</param>
        private RemoteContextModel ReadContext(BinaryReader reader)
        {
            string data = reader.ReadString();
            RemoteContextModel model = this.JsonHelper.Deserialize<RemoteContextModel>(data);
            return model.ApiVersion != null
                ? model
                : null; // no data available for unmodded players
        }

        /// <summary>Receive a mod message sent from another player's mods.</summary>
        /// <param name="message">The raw message to parse.</param>
        private void ReceiveModMessage(IncomingMessage message)
        {
            // @todo
        }

        /// <summary>Get the screen ID for a given player ID, if the player is local.</summary>
        /// <param name="playerId">The player ID to check.</param>
        private int? GetScreenId(long playerId)
        {
            return 0; // @todo SGameRunner.Instance.GetScreenId(playerId);
        }

        /// <summary>Get all connected player IDs, including the current player.</summary>
        private IEnumerable<long> GetKnownPlayerIDs()
        {
            // @todo yield return Game1.player.UniqueMultiplayerID;
            foreach (long peerID in this.Peers.Keys)
                yield return peerID;
        }

        /// <summary>Get the fields to include in a context sync message sent to other players.</summary>
        private object[] GetContextSyncMessageFields()
        {
            // @todo

            return new object[] { };
        }

        /// <summary>Get the fields to include in a context sync message sent to other players.</summary>
        /// <param name="peer">The peer whose data to represent.</param>
        private object[] GetContextSyncMessageFields(IMultiplayerPeer peer)
        {
            if (!peer.HasSmapi)
                return new object[] { "{}" };

            RemoteContextModel model = new RemoteContextModel
            {
                IsHost = peer.IsHost,
                Platform = peer.Platform.Value,
                ApiVersion = peer.ApiVersion,
                GameVersion = peer.GameVersion,
                Mods = peer.Mods
                    .Select(mod => new RemoteContextModModel
                    {
                        ID = mod.ID,
                        Name = mod.Name,
                        Version = mod.Version
                    })
                    .ToArray()
            };

            return new object[] { this.JsonHelper.Serialize(model, Formatting.None) };
        }
    }
}
