namespace MironovLab.OpenVPN.Management.Exceptions
{
    public class AuthenticationException : OvpnManagementException
    {
        public AuthenticationException(string message) : base(message)
        {
        }

        internal static AuthenticationException Create(string message)
        {
            return new AuthenticationException(string.Format(Resources.AuthenticationFailed, message));
        }
    }
}
