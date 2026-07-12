namespace MironovLab.OpenVPN.Management
{
    internal static class Constants
    {
        public const char Slash = '\\';
        public const char SingleQuote = '\'';
        public const char DoubleQuote = '"';
        public const char WhiteSpace = ' ';
        public const char RealTimeMessageSign = '>';
        public const char SourceTypeDataSplitter = ':';
        public const char MessageParamSplitter = ',';
        public const char NameValueSplitter = '=';
        public const char SectionEnclosureStart = '[';
        public const char SectionEnclosureEnd = ']';
        public const string NewLine = "\r\n";
        public const string EnterPassword = "ENTER PASSWORD";
        public const string EndOfResult = "END";
        public const string TableHeader = "HEADER";
        public const int DefaultLogFileHistoryCacheSize = 250;
        public const int ResponseWaitingMSec = 100;

        internal static class Commands
        {
            public const string ByteCount = "bytecount";
            public const string Exit = "exit";
            public const string Echo = "echo";
            public const string Hold = "hold";
            public const string Kill = "kill";
            public const string Mute = "mute";
            public const string Pid = "pid";
            public const string UserName = "username";
            public const string Password = "password";
            public const string ForgetPasswords = "forget-passwords";
            public const string Signal = "signal";
            public const string State = "state";
            public const string Status = "status";
            public const string Log = "log";
            public const string Verb = "verb";
            public const string Version = "version";
            public const string AuthRetry = "auth-retry";
            public const string NeedOk = "needok";
            public const string NeedStr = "needstr";
            public const string PKCS11IdCount = "pkcs11-id-count";
            public const string PKCS11IdGet = "pkcs11-id-get";
            public const string ClientAuth = "client-auth";
            public const string ClientAuthNt = "client-auth-nt";
            public const string ClientDeny = "client-deny";
            public const string ClientKill = "client-kill";
            public const string ClientPacketFilter = "client-pf";
            public const string Remote = "remote";
            public const string Proxy = "proxy";
            public const string RSASign = "rsa-sig";
        }

        internal static class CommandArguments
        {
            public const string On = "on";
            public const string Off = "off";
            public const string All = "all";
            public const string Release = "release";
            public const string PasswordTypePrivateKey = "Private Key";
            public const string PasswordTypeAuth = "Auth";
            public const string SignalHangUp = "SIGHUP";
            public const string SignalTerminate = "SIGTERM";
            public const string SignalUserDefined1 = "SIGUSR1";
            public const string SignalUserDefined2 = "SIGUSR2";
            public const string AuthRetryTypeNone = "none";
            public const string AuthRetryTypeNoInteract = "nointeract";
            public const string AuthRetryTypeInteract = "interact";
            public const string NeedOkTokenInsertionRequest = "token-insertion-request";
            public const string NeedOkResponseOk = "ok";
            public const string NeedOkResponseCancel = "cancel";
            public const string RemoteAccept = "ACCEPT";
            public const string RemoteModify = "MOD";
            public const string RemoteSkip = "SKIP";
            public const string ProxyNone = "NONE";
            public const string ProxyHttp = "HTTP";
            public const string ProxySocks = "SOCKS";
            public const string ProxyNonClearText = "nct";
        }

        internal static class MessageTypes
        {
            public const string Success = "SUCCESS";
            public const string Error = "ERROR";
            public const string PKCS11IdCount = "PKCS11ID-COUNT";
            public const string PKCS11IdGet = "PKCS11ID-ENTRY";
        }

        internal static class RealTimeMessages
        {
            public const string ByteCount = "BYTECOUNT";
            public const string ByteCountCli = "BYTECOUNT_CLI";
            public const string Client = "CLIENT";
            public const string Echo = "ECHO";
            public const string Fatal = "FATAL";
            public const string Hold = "HOLD";
            public const string Info = "INFO";
            public const string Log = "LOG";
            public const string NeedOk = "NEED-OK";
            public const string NeedStr = "NEED-STR";
            public const string Password = "PASSWORD";
            public const string State = "STATE";
            public const string Notify = "NOTIFY";
        }
    }
}
