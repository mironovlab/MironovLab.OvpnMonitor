using System;
using System.IO;
using System.Linq;
using System.Threading;
using MironovLab.OpenVPN.Management;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Common.Pf;
using MironovLab.OvpnMonitor.Tests.TestData;
using NUnit.Framework;

namespace MironovLab.OvpnMonitor.Tests
{
    public class OvpnPasswordProtectedTests
    {
        private OvpnManager _manager;
        [SetUp]
        public void Setup()
        {
            _manager = new OvpnManager();
            var result = _manager.Connect("10.8.0.1", 5555, "Pa$$w0rd");

        }

        [TearDown]
        public void Teardown()
        {
            _manager.Dispose();
        }

        [Test]
        public void Test1()
        {
            var status = _manager.Status();
            var result = _manager.Log(Switch.On);
            result = _manager.Hold(Switch.Release);
            var hold = _manager.Hold();
            var mute = _manager.Mute();
            result = _manager.Mute(mute);
            _manager.LogReceived += (sender, args) =>
            {
                if (args.LogRecord.MessageText.Length > 1000)
                    Console.WriteLine($"{args.LogRecord.DateTime:s} {args.LogRecord.MessageText}");
            };
            _manager.ClientReceived += (sender, client) =>
            {
                var clientData = client.GetEnvironmentVariablesReader();
            };
            var logs = _manager.Log(true);
            logs = logs.Where(x => x.MessageText.Length > 1000).ToList();
            Thread.Sleep(TimeSpan.FromMinutes(5));
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
        public void PKCS11IdCountTest()
        {
            var count = _manager.PKCS11IdCount();
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
                var result = _manager.Connect("localhost", 5555, "Pa$$w0rd");
                var version = _manager.Version();
            };
            var version = _manager.Version();
        }

        [Test]
        public void RSASignTest()
        {
            var data = new byte[1024];
            new Random().NextBytes(data);
            _manager.RSASign(data);
        }
    }
}