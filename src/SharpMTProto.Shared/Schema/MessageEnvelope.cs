﻿//////////////////////////////////////////////////////////
// Copyright (c) Alexander Logger. All rights reserved. //
//////////////////////////////////////////////////////////

namespace SharpMTProto.Schema
{
    using System;

    public interface IMessageEnvelope
    {
        /// <summary>
        ///     Key Identifier. The 64 lower-order bits of the SHA1 hash of the authorization key
        ///     are used to indicate which particular key was used to encrypt a message.
        ///     Keys must be uniquely defined by the 64 lower-order bits of their SHA1,
        ///     and in the event of a collision, an authorization key is regenerated.
        ///     A zero key identifier means that encryption is not used which is permissible
        ///     for a limited set of message types used during registration to generate
        ///     an authorization key based on a Diffie-Hellman exchange.
        /// </summary>
        ulong AuthKeyId { get; }

        /// <summary>
        ///     Session is a (random) 64-bit number generated by the client to distinguish between individual sessions (for
        ///     example, between different instances of the application, created with the same authorization key). The session in
        ///     conjunction with the key identifier corresponds to an application instance. The server can maintain session state.
        ///     Under no circumstances can a message meant for one session be sent into a different session. The server may
        ///     unilaterally forget any client sessions; clients should be able to handle this.
        /// </summary>
        ulong SessionId { get; }

        /// <summary>
        ///     Server Salt is a (random) 64-bit number periodically (say, every 24 hours) changed (separately for
        ///     each session) at the request of the server. All subsequent messages must contain the new salt (although, messages
        ///     with the old salt are still accepted for a further 300 seconds). Required to protect against replay attacks and
        ///     certain tricks associated with adjusting the client clock to a moment in the distant future.
        /// </summary>
        ulong Salt { get; }

        /// <summary>
        ///     Message.
        /// </summary>
        IMessage Message { get; }

        /// <summary>
        ///     Is message encrypted.
        /// </summary>
        bool IsEncrypted { get; }
    }

    public class MessageEnvelope : IEquatable<MessageEnvelope>, IMessageEnvelope
    {
        /// <summary>
        ///     Initializes a new <see cref="MessageEnvelope" /> for a plain message with zero auth key id.
        /// </summary>
        /// <param name="message">A message.</param>
        public MessageEnvelope(IMessage message)
        {
            AuthKeyId = 0;
            SessionId = 0;
            Salt = 0;
            Message = message;
        }

        /// <summary>
        ///     Initializes a new <see cref="MessageEnvelope" /> for an encrypted message.
        /// </summary>
        /// <param name="authKeyId">Auth key id.</param>
        /// <param name="salt">Salt.</param>
        /// <param name="sessionId">Session id.</param>
        /// <param name="message">A message.</param>
        public MessageEnvelope(ulong authKeyId, ulong sessionId, ulong salt, IMessage message)
        {
            AuthKeyId = authKeyId;
            SessionId = sessionId;
            Salt = salt;
            Message = message;
        }

        public ulong AuthKeyId { get; private set; }
        public ulong SessionId { get; private set; }
        public ulong Salt { get; private set; }
        public IMessage Message { get; private set; }

        public bool IsEncrypted
        {
            get { return AuthKeyId != 0; }
        }

        #region Equality

        public bool Equals(MessageEnvelope other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return AuthKeyId == other.AuthKeyId && SessionId == other.SessionId && Salt == other.Salt && Equals(Message, other.Message);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((MessageEnvelope) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = AuthKeyId.GetHashCode();
                hashCode = (hashCode*397) ^ SessionId.GetHashCode();
                hashCode = (hashCode*397) ^ Salt.GetHashCode();
                hashCode = (hashCode*397) ^ (Message != null ? Message.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(MessageEnvelope left, MessageEnvelope right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MessageEnvelope left, MessageEnvelope right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}