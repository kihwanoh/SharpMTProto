﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MTProtoBuilderFacts.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using FluentAssertions;
using NUnit.Framework;
using SharpMTProto.Transport;

namespace SharpMTProto.Tests
{
    [TestFixture]
    [Category("Core")]
    public class MTProtoClientBuilderFacts
    {
        [Test]
        public void Should_create_connection()
        {
            IMTProtoClientConnection connection = MTProtoClientBuilder.BuildConnection(new TcpClientTransportConfig("127.0.0.1", 9999));
            connection.Should().NotBeNull();
        }
        
        [Test]
        public void Should_create_auth_key_negotiator()
        {
            var authKeyNegotiator = MTProtoClientBuilder.BuildAuthKeyNegotiator(new TcpClientTransportConfig("127.0.0.1", 9999));
            authKeyNegotiator.Should().NotBeNull();
        }
    }
}