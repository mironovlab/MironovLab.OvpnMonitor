using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MironovLab.OpenVPN.Management.Exceptions;

namespace MironovLab.OpenVPN.Management.Core
{
    internal static class ParseUtils
    {
        private static readonly char[] DataSeparator =
        {
            Constants.MessageParamSplitter,
        };

        private static readonly char[] RealTimeMessageDataSeparator =
        {
            Constants.SourceTypeDataSplitter,
            Constants.MessageParamSplitter,
        };

        public static DateTime DateTimeFromUnixTime(long unixTime)
        {
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        public static DateTime DateTimeFromUnixTime(string unixTime)
        {
            return DateTimeFromUnixTime(long.Parse(unixTime));
        }

        public static string[] SplitDataParts(string dataLine)
        {
            return dataLine.Split(DataSeparator);
        }

        public static List<string> SplitDataParts(string source, char splitter, char enclosureStart, char enclosureEnd)
        {
            var startIndex = source[0] == Constants.RealTimeMessageSign ? 1 : 0;
            var result = new List<string>();

            while (true)
            {
                var enclosed = source[startIndex] == enclosureStart;
                var splitterSearchIndex = enclosed
                    ? source.IndexOf(enclosureEnd, startIndex + 1)
                    : startIndex;

                if (source.IndexOf(splitter, splitterSearchIndex) is var pos && pos < 0)
                    pos = source.Length;

                var substr = source.Substring(
                    enclosed ? startIndex + 1 : startIndex,
                    enclosed ? pos - startIndex - 2 : pos - startIndex);

                if (!string.IsNullOrEmpty(substr))
                    result.Add(substr);

                startIndex = pos + 1;
                if (startIndex >= source.Length)
                    break;
            }

            return result;
        }

        public static string[] SplitRealTimeMessageDataParts(string dataLine)
        {
            return dataLine.Split(RealTimeMessageDataSeparator);
        }

        public static void ParseSimpleMessage(string dataLine, out string messageType, out string text)
        {
            messageType = dataLine;
            text = string.Empty;
            var pos = dataLine.IndexOf(Constants.SourceTypeDataSplitter);
            if (pos < 0)
                return;

            var startIndex = dataLine[0] == Constants.RealTimeMessageSign ? 1 : 0;
            messageType = dataLine.Substring(startIndex, pos - startIndex);
            text = dataLine.Substring(pos + 1).Trim();
        }

        public static IEnumerable<KeyValuePair<string, string>> ParseKeyValuePairs(string data)
        {
            var sb = new StringBuilder(data.Length);
            var startIndex = 0;
            while (true)
            {
                var pos = data.IndexOf(Constants.SourceTypeDataSplitter, startIndex);
                if (pos < 0)
                    yield break;
                var key = data.Substring(startIndex, pos - startIndex).Trim();
                startIndex = pos + 1;
                while (startIndex < data.Length && data[startIndex] == Constants.WhiteSpace)
                    startIndex++;

                if (startIndex >= data.Length)
                {
                    yield return new KeyValuePair<string, string>(key, sb.ToString());
                    yield break;
                }

                var enclosingSymbol = char.MinValue;
                if (data[startIndex] is var c && (c == Constants.SingleQuote || c == Constants.DoubleQuote))
                {
                    enclosingSymbol = c;
                    startIndex++;
                }

                var escapedSymbol = false;
                for (var i = startIndex; i < data.Length; i++)
                {
                    c = data[i];
                    if (escapedSymbol)
                    {
                        sb.Append(c);
                        escapedSymbol = false;
                    }
                    else if (c == Constants.Slash)
                    {
                        escapedSymbol = true;
                    }
                    else if (enclosingSymbol != char.MinValue)
                    {
                        if (c == enclosingSymbol)
                        {
                            startIndex = i + 1;
                            goto nextItem;
                        }

                        sb.Append(c);
                    }
                    else switch (c)
                    {
                        case Constants.MessageParamSplitter:
                            startIndex = i;
                            goto nextItem;
                        case Constants.WhiteSpace:
                            startIndex = i + 1;
                            goto nextItem;
                        default:
                            sb.Append(c);
                            break;
                    }
                }
                nextItem:
                pos = data.IndexOf(Constants.MessageParamSplitter, startIndex);
                if (pos < 0)
                    startIndex = data.Length;
                else
                    startIndex = pos + 1;
                yield return new KeyValuePair<string, string>(key, sb.ToString());
                sb.Clear();
            }
        }

        public static int SequentialSplit(string source, char splitter, string[] splitResult, int fillCountMax = 0)
        {
            return SequentialSplit(source, splitter, splitResult, char.MinValue, char.MinValue, fillCountMax);
        }

        public static int SequentialSplit(string source, char splitter, string[] splitResult, char enclosureStart, char enclosureEnd, int fillCountMax = 0)
        {
            var startIndex = source[0] == Constants.RealTimeMessageSign ? 1 : 0;
            var i = 0;
            var lastItemIndex = fillCountMax <= 0 || fillCountMax > splitResult.Length
                ? splitResult.Length - 1
                : fillCountMax - 1;

            while (true)
            {
                var enclosed = source[startIndex] == enclosureStart;
                var splitterSearchIndex = enclosed
                    ? source.IndexOf(enclosureEnd, startIndex + 1)
                    : startIndex;

                if (i == lastItemIndex || source.IndexOf(splitter, splitterSearchIndex) is var pos && pos < 0)
                {
                    splitResult[i] = source.Substring(
                        enclosed ? startIndex + 1 : startIndex,
                        enclosed ? source.Length - startIndex - 2 : source.Length - startIndex);
                    return i + 1;
                }

                splitResult[i] = source.Substring(
                    enclosed ? startIndex + 1 : startIndex,
                    enclosed ? pos - startIndex - 2 : pos - startIndex);
                startIndex = pos + 1;
                i++;
            }
        }

        public static IPAddress ParseIPAddressSafe(string ipAddress)
        {
            IPAddress.TryParse(ipAddress, out var address);
            return address;
        }

        public static string ToString(object obj)
        {
            return ToString(obj, false);
        }

        public static string ToStringWithQuotes(object obj)
        {
            return ToString(obj, true);
        }

        public static string ToString(object obj, bool withQuotes)
        {
            switch (obj)
            {
                case null:
                    return null;
                case string str:
                    if (Constants.NewLine.Any(x => str.Contains(x)))
                        throw new ArgumentException("Arguments should not contain new line characters!");

                    return withQuotes ? EscapeWithQuotes(str) : EscapeNoQuotes(str);
                case int i:
                    return i.ToString("D");
                case long l:
                    return l.ToString("D");
                case IPEndPoint ep:
                    return ep.ToString();
                default:
                    throw CommandArgumentTypeUnsupportedException.Create(obj.GetType());
            }
        }

        public static string EscapeNoQuotes(string value)
        {
            var sb = new StringBuilder(value.Length);

            foreach (var chr in value)
            {
                if (chr == Constants.Slash || chr == Constants.SingleQuote || chr == Constants.DoubleQuote || chr == Constants.WhiteSpace)
                    sb.Append(Constants.Slash);
                sb.Append(chr);
            }

            return sb.ToString();
        }

        public static string EscapeWithQuotes(string value)
        {
            var sb = new StringBuilder(value.Length);
            sb.Append(Constants.DoubleQuote);

            foreach (var chr in value)
            {
                if (chr == Constants.Slash || chr == Constants.SingleQuote || chr == Constants.DoubleQuote)
                    sb.Append(Constants.Slash);
                sb.Append(chr);
            }

            sb.Append(Constants.DoubleQuote);
            return sb.ToString();
        }

        public static string SanitizeNewLine(string value)
        {
            if (value == null)
                return null;

            var newLineIndex = -1;
            foreach (var c in Constants.NewLine)
            {
                var pos = value.IndexOf(c);
                if (pos >= 0 && (newLineIndex < 0 || newLineIndex > pos))
                    newLineIndex = pos;
            }

            return newLineIndex < 0 ? value : value.Substring(0, newLineIndex);
        }

        public static IEnumerable<string> SplitAndSanitize(string data)
        {
            return data
                .Split(new[] { Constants.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => SanitizeNewLine(x)?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x));
        }

        public static IPEndPoint ParseIPEndPoint(string ipEndpoint)
        {
            var pos = ipEndpoint.LastIndexOf(':');
            if (pos >= 0)
            {
                var ipAddress = ipEndpoint.Substring(0, pos);
                var port = ipEndpoint.Substring(pos + 1);
                try
                {
                    return new IPEndPoint(IPAddress.Parse(ipAddress), int.Parse(port));
                }
                catch (FormatException)
                {
                    // The column "Real Address" contains address with port when the address is IPv4 and IPv6 address only when the address is IPv6
                    return new IPEndPoint(IPAddress.Parse(ipEndpoint), 0);
                }
            }

            return new IPEndPoint(IPAddress.Parse(ipEndpoint), 0);
        }
    }
}
