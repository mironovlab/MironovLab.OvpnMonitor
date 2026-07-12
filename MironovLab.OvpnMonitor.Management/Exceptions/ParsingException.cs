using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MironovLab.OpenVPN.Management.Exceptions
{
    public class ParsingException : OvpnManagementException
    {
        public ParsingException(string message) : base(message)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ParsingException Create(string line)
        {
            return new ParsingException(string.Format(Resources.RealTimeMessageParsingException, line, GetParserName()));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetParserName()
        {
            var methodInfo = new StackTrace().GetFrame(2)?.GetMethod();
            return methodInfo?.ReflectedType?.Name ?? "<UNKNOWN>";
        }
    }
}
