﻿using System.Collections.Generic;
using SoGModdingAPI.Framework.Networking;

namespace SoGModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides multiplayer utilities.</summary>
    internal class MultiplayerHelper : BaseHelper, IMultiplayerHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>SMAPI's core multiplayer utility.</summary>
        private readonly SMultiplayer Multiplayer;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The unique ID of the relevant mod.</param>
        /// <param name="multiplayer">SMAPI's core multiplayer utility.</param>
        public MultiplayerHelper(string modID, SMultiplayer multiplayer)
            : base(modID)
        {
            this.Multiplayer = multiplayer;
        }

        /// <inheritdoc />
        public long GetNewID()
        {
            return 0; // @todo this.Multiplayer.getNewID();
        }

        /// <inheritdoc />
        public IEnumerable<GameLocation> GetActiveLocations()
        {
            return new GameLocation[]{ };// @todo  this.Multiplayer.activeLocations();
        }

        /// <inheritdoc />
        public IMultiplayerPeer GetConnectedPlayer(long id)
        {
            return this.Multiplayer.Peers.TryGetValue(id, out MultiplayerPeer peer)
                ? peer
                : null;
        }

        /// <inheritdoc />
        public IEnumerable<IMultiplayerPeer> GetConnectedPlayers()
        {
            return this.Multiplayer.Peers.Values;
        }

        /// <inheritdoc />
        public void SendMessage<TMessage>(TMessage message, string messageType, string[] modIDs = null, long[] playerIDs = null)
        {
            this.Multiplayer.BroadcastModMessage(
                message: message,
                messageType: messageType,
                fromModID: this.ModID,
                toModIDs: modIDs,
                toPlayerIDs: playerIDs
            );
        }
    }
}
