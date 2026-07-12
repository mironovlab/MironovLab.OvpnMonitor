using System;
using System.IO;
using System.Threading;
using MironovLab.OpenVPN.Management;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Common.Pf;
using MironovLab.OvpnMonitor.Tests.TestData;
using NUnit.Framework;

namespace MironovLab.OvpnMonitor.Tests
{
    public class OvpnNoPasswordProtectedTests
    {
        private OvpnManager _manager;
        [SetUp]
        public void Setup()
        {
            _manager = new OvpnManager();
            _manager.Connect("localhost", 5555);
        }

        [TearDown]
        public void Teardown()
        {
            _manager.Dispose();
        }

        [Test]
        public void TestState()
        {
            var state = _manager.State();
            var states = _manager.State(null);
        }

        [Test]
        public void TestVersion()
        {
            var version = _manager.Version();
        }

        [Test]
        public void AuthRetryTest()
        {
            var retryType = _manager.AuthRetry();
            var result = _manager.AuthRetry(AuthRetryType.None);
            Assert.That(_manager.AuthRetry(), Is.EqualTo(AuthRetryType.None));
            _manager.AuthRetry(AuthRetryType.NoInteract);
            Assert.That(_manager.AuthRetry(), Is.EqualTo(AuthRetryType.NoInteract));
            _manager.AuthRetry(AuthRetryType.Interact);
            Assert.That(_manager.AuthRetry(), Is.EqualTo(AuthRetryType.Interact));
            _manager.AuthRetry(retryType);
        }

        [Test]
        public void ClientPacketFilterTest()
        {
            var pf = PacketFilter.Parse(File.ReadAllText(TestFileLoader.GetTestDataFileName("FilterSpec1.txt")));
            _manager.ClientPacketFilter(1, pf);
        }

        [Test]
        public void ReconnectionTest()
        {
            _manager.Disconnected += (sender, args) =>
            {
                _manager.Connect("localhost", 5555);
                var version = _manager.Version();
            };
            Thread.Sleep(TimeSpan.FromMinutes(3));
            var version = _manager.Version();
        }
    }
}
