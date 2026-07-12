using System;

namespace MironovLab.OpenVPN.Management.Exceptions
{
    public class OvpnManagementException : Exception
    {
        public OvpnManagementException(string message) : base(message)
        {
        }
    }
}
