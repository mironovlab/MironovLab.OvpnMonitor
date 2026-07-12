using System;

namespace MironovLab.OpenVPN.Management.Common
{
    public readonly struct EchoItem
    {
        public readonly DateTime DateTime;
        public readonly string Text;

        public EchoItem(DateTime dateTime, string text)
        {
            DateTime = dateTime;
            Text = text;
        }
    }
}
