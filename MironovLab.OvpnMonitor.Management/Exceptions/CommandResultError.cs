namespace MironovLab.OpenVPN.Management.Exceptions
{
    public class CommandResultError : OvpnManagementException
    {
        public CommandResultError(string message) : base(message)
        {
        }
    }
}
