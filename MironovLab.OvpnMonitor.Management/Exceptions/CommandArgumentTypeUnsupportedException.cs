using System;

namespace MironovLab.OpenVPN.Management.Exceptions
{
    public class CommandArgumentTypeUnsupportedException : OvpnManagementException
    {
        public CommandArgumentTypeUnsupportedException(string message) : base(message)
        {
        }

        internal static CommandArgumentTypeUnsupportedException Create(Type type)
        {
            return new CommandArgumentTypeUnsupportedException(string.Format(Resources.CommandArgumentTypeUnsupportedException, type));
        }
    }
}
