using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using MironovLab.OpenVPN.Management.Common;
using MironovLab.OpenVPN.Management.Common.Pf;
using MironovLab.OpenVPN.Management.Core;
using MironovLab.OpenVPN.Management.RealTimeMessages.Parsers;
using MironovLab.OvpnMonitor.Tests.TestData;
using NUnit.Framework;

namespace MironovLab.OvpnMonitor.Tests
{
    public class ParsersTests
    {
        [Test]
        public void EchoTest()
        {
            var echoParser = new EchoParser();
            echoParser.Parse(">ECHO:1101519562,forget-passwords");
            Assert.That(echoParser.ObjectIsReady, Is.True);
            var message = echoParser.ParsedMessage;
            Assert.That(message, Is.Not.Null);
            Assert.That(message.EchoItem.Text, Is.EqualTo("forget-passwords"));
        }

        [Test]
        public void IPNetworkTest()
        {
            var net = IPNetwork2.Parse("1.2.3.0/255.255.255.0");
        }

        [Test]
        public void KeyValuePairsParserTest()
        {
            var result = ParseUtils.ParseKeyValuePairs("PKCS11ID-ENTRY:'1', ID:'kirill \\'mironov', BLOB:,FUCK:''").ToList();
        }

        [Test]
        public void PacketFilterClassesTest()
        {
            var clients = new Clients(PolicyType.Accept)
            {
                { PolicyType.Accept, "Kirill" },
                { PolicyType.Drop, "Pavel" },
            };

            var networks = new Subnets(PolicyType.Drop)
            {
                PolicyType.Drop,
                { PolicyType.Drop, "1.2.3.4" },
                { PolicyType.Accept, "192.168.115.0/255.255.255.0" },
                { PolicyType.Accept , "fe80::739a:738b:0000:0000/96"},
                "-unknown",
                "-1.2.3.4",
                "+192.168.115.0/255.255.255.0",
                "+ fe80::739a:738b:a04e:4406/128",
                "-fe80::739a:738b:0000:0000/96",
            };

            var filter = new PacketFilter(clients, networks);
        }

        [Test]
        [TestCase("FilterSpec1.txt", true)]
        [TestCase("FilterSpec2.txt", true)]
        public void PacketFilterParsingTest(string fileName, bool valid)
        {
            var result = PacketFilter.TryParse(File.ReadAllText(TestFileLoader.GetTestDataFileName(fileName)), out var pf);
            Assert.That(result, Is.EqualTo(valid));
        }

        [Test]
        [TestCase("", false)]
        [TestCase("example.com", true)]
        [TestCase("example.com ", false)]
        [TestCase(" example.com", false)]
        [TestCase(" ", false)]
        [TestCase("хуй.ru", false)]
        [TestCase("sd.example.com", true)]
        [TestCase("example", true)]
        [TestCase("питух", false)]
        public void TestRegexp(string domain, bool valid)
        {
            var regexp = new Regex("^([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\\-]{0,61}[a-zA-Z0-9])(\\.([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\\-]{0,61}[a-zA-Z0-9]))*$");
            var result = regexp.IsMatch(domain);
            Assert.That(result, Is.EqualTo(valid));
        }

        [Test]
        [TestCase(">PASSWORD:Need 'Private Key' password", PasswordMessageType.Need, PasswordType.PrivateKey, null)]
        [TestCase(">PASSWORD:Need 'Auth' username/password", PasswordMessageType.Need, PasswordType.Auth, null)]
        [TestCase(">PASSWORD:Verification Failed: 'Private Key'", PasswordMessageType.VerificationFailed, PasswordType.PrivateKey, null)]
        [TestCase(">PASSWORD:Verification Failed: 'Auth'", PasswordMessageType.VerificationFailed, PasswordType.Auth, null)]
        [TestCase(">PASSWORD:Verification Failed: 'custom server-generated string'", PasswordMessageType.VerificationFailed, PasswordType.CustomText, "custom server-generated string")]
        [TestCase("PASSWORD:Need 'Private Key' password", PasswordMessageType.Need, PasswordType.PrivateKey, null)]
        [TestCase("PASSWORD:Need 'Auth' username/password", PasswordMessageType.Need, PasswordType.Auth, null)]
        [TestCase("PASSWORD:Verification Failed: 'Private Key'", PasswordMessageType.VerificationFailed, PasswordType.PrivateKey, null)]
        [TestCase("PASSWORD:Verification Failed: 'Auth'", PasswordMessageType.VerificationFailed, PasswordType.Auth, null)]
        [TestCase("PASSWORD:Verification Failed: 'custom server-generated string'", PasswordMessageType.VerificationFailed, PasswordType.CustomText, "custom server-generated string")]
        public void PasswordParserTest(string pwdLine, PasswordMessageType msgType, PasswordType pwdType, string customText)
        {
            var parser = new PasswordParser();
            parser.Parse(pwdLine);
            Assert.That(parser.ObjectIsReady);
            var password = parser.ParsedMessage;
            Assert.That(password.MessageType, Is.EqualTo(msgType));
            Assert.That(password.PasswordType, Is.EqualTo(pwdType));
            Assert.That(password.CustomText, Is.EqualTo(customText));
        }

        [Test]
        [TestCase(">NEED-OK:Need 'token-insertion-request' confirmation MSG:Please insert your cryptographic token", NeedOkRequestType.TokenInsertionRequest, "Please insert your cryptographic token")]
        [TestCase("NEED-OK:Need 'token-insertion-request' confirmation MSG:Please insert your cryptographic token", NeedOkRequestType.TokenInsertionRequest, "Please insert your cryptographic token")]
        public void NeedOkParserTest(string needOkLine, NeedOkRequestType requestType, string userMessage)
        {
            var parser = new NeedOkParser();
            parser.Parse(needOkLine);
            Assert.That(parser.ObjectIsReady);
            var password = parser.ParsedMessage;
            Assert.That(password.RequestType, Is.EqualTo(requestType));
            Assert.That(password.UserMessage, Is.EqualTo(userMessage));
        }

        [Test]
        [TestCase(">NEED-STR:Need 'name' input MSG:Please specify your name", "name", "Please specify your name")]
        [TestCase("NEED-STR:Need 'name' input MSG:Please specify your name", "name", "Please specify your name")]
        public void NeedStrParserTest(string needStrLine, string inputType, string userMessage)
        {
            var parser = new NeedStrParser();
            parser.Parse(needStrLine);
            Assert.That(parser.ObjectIsReady);
            var password = parser.ParsedMessage;
            Assert.That(password.InputType, Is.EqualTo(inputType));
            Assert.That(password.UserMessage, Is.EqualTo(userMessage));
        }

        [Test]
        [TestCase(">NOTIFY:info,remote-exit,EXIT", NotifySeverity.Info, "info", NotifyType.RemoteExit, "remote-exit", "EXIT")]
        [TestCase(">NOTIFY:warning,remote-logon,LOGON", NotifySeverity.Other, "warning", NotifyType.Other, "remote-logon", "LOGON")]
        [TestCase(">NOTIFY:error", NotifySeverity.Other, null, NotifyType.Other, null, null)]
        public void NotifyTest(string notifyStrLine, NotifySeverity severity, string severityText, NotifyType type, string typeText, string text)
        {
            var parser = new NotifyParser();
            parser.Parse(notifyStrLine);
            Assert.That(parser.ObjectIsReady);
            var notify = parser.ParsedMessage;
            Assert.That(notify.Severity, Is.EqualTo(severity));
            Assert.That(notify.SeverityText, Is.EqualTo(severityText));
            Assert.That(notify.NotifyType, Is.EqualTo(type));
            Assert.That(notify.NotifyTypeText, Is.EqualTo(typeText));
            Assert.That(notify.Text, Is.EqualTo(text));
        }
    }
}
