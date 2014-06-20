﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AuthKeyNegotiatorFacts.cs">
//   Copyright (c) 2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using BigMath.Utils;
using Catel.IoC;
using Catel.Logging;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpMTProto.Messages;
using SharpMTProto.Services;
using SharpMTProto.Transport;
using SharpTL;

namespace SharpMTProto.Tests
{
    [TestFixture]
    public class AuthKeyNegotiatorFacts
    {
        [SetUp]
        public void SetUp()
        {
            LogManager.AddDebugListener(true);
        }

        [Test]
        public async Task Should_create_auth_key()
        {
            TimeSpan defaultRpcTimeout = TimeSpan.FromSeconds(5);
            TimeSpan defaultConnectTimeout = TimeSpan.FromSeconds(5);

            var serviceLocator = ServiceLocator.Default;
            var typeFactory = this.GetTypeFactory();

            serviceLocator.RegisterInstance(Mock.Of<TransportConfig>());
            serviceLocator.RegisterInstance(TLRig.Default);
            serviceLocator.RegisterInstance<IMessageIdGenerator>(new TestMessageIdsGenerator());
            serviceLocator.RegisterInstance<INonceGenerator>(new TestNonceGenerator());
            serviceLocator.RegisterType<IHashServices, HashServices>();
            serviceLocator.RegisterType<IEncryptionServices, EncryptionServices>();
            serviceLocator.RegisterType<IRandomGenerator, RandomGenerator>();
            serviceLocator.RegisterType<IMessageProcessor, MessageProcessor>();
            serviceLocator.RegisterType<IMTProtoConnection, MTProtoConnection>(RegistrationType.Transient);
            serviceLocator.RegisterType<IMTProtoConnectionFactory, MTProtoConnectionFactory>();
            serviceLocator.RegisterType<IKeyChain, KeyChain>();

            var inTransport = new Subject<byte[]>();
            var mockTransport = new Mock<ITransport>();
            mockTransport.Setup(transport => transport.Subscribe(It.IsAny<IObserver<byte[]>>())).Callback<IObserver<byte[]>>(observer => inTransport.Subscribe(observer));
            mockTransport.Setup(transport => transport.SendAsync(TestData.ReqPQ, It.IsAny<CancellationToken>())).Callback(() => inTransport.OnNext(TestData.ResPQ)).Returns(() => Task.FromResult(false));
            mockTransport.Setup(transport => transport.SendAsync(TestData.ReqDHParams, It.IsAny<CancellationToken>())).Callback(() => inTransport.OnNext(TestData.ServerDHParams)).Returns(() => Task.FromResult(false));
            mockTransport.Setup(transport => transport.SendAsync(TestData.SetClientDHParams, It.IsAny<CancellationToken>())).Callback(() => inTransport.OnNext(TestData.DhGenOk)).Returns(() => Task.FromResult(false));

            var mockTransportFactory = new Mock<ITransportFactory>();
            mockTransportFactory.Setup(factory => factory.CreateTransport(It.IsAny<TransportConfig>())).Returns(mockTransport.Object);

            var mockEncryptionServices = new Mock<IEncryptionServices>();
            mockEncryptionServices.Setup(services => services.RSAEncrypt(It.IsAny<byte[]>(), It.IsAny<PublicKey>())).Returns(TestData.EncryptedData);
            mockEncryptionServices.Setup(services => services.Aes256IgeDecrypt(TestData.ServerDHParamsOkEncryptedAnswer, TestData.TmpAesKey, TestData.TmpAesIV))
                .Returns(TestData.ServerDHInnerDataWithHash);
            mockEncryptionServices.Setup(
                services =>
                    services.Aes256IgeEncrypt(It.Is<byte[]>(bytes => bytes.RewriteWithValue(0, bytes.Length - 12, 12).SequenceEqual(TestData.ClientDHInnerDataWithHash)),
                        TestData.TmpAesKey, TestData.TmpAesIV)).Returns(TestData.SetClientDHParamsEncryptedData);
            mockEncryptionServices.Setup(services => services.DH(TestData.B, TestData.G, TestData.GA, TestData.P)).Returns(new DHOutParams(TestData.GB, TestData.AuthKey));

            serviceLocator.RegisterInstance(Mock.Of<ITransportConfigProvider>(provider => provider.DefaultTransportConfig == Mock.Of<TransportConfig>()));
            serviceLocator.RegisterInstance(mockTransportFactory.Object);
            serviceLocator.RegisterInstance(mockEncryptionServices.Object);
            

            var keyChain = serviceLocator.ResolveType<IKeyChain>();
            keyChain.AddKeys(TestData.TestPublicKeys);

            var connectionFactory = serviceLocator.ResolveType<IMTProtoConnectionFactory>();
            connectionFactory.DefaultRpcTimeout = defaultRpcTimeout;
            connectionFactory.DefaultConnectTimeout = defaultConnectTimeout;

            var authKeyNegotiator = typeFactory.CreateInstance<AuthKeyNegotiator>();

            var authInfo = await authKeyNegotiator.CreateAuthKey();
            authInfo.AuthKey.ShouldAllBeEquivalentTo(TestData.AuthKey);
            authInfo.Salt.Should().Be(TestData.InitialSalt);
        }
    }
}
