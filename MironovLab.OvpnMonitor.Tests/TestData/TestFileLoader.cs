using System.IO;
using System.Reflection;

namespace MironovLab.OvpnMonitor.Tests.TestData
{
    internal static class TestFileLoader
    {
        public static string GetTestDataFileName(string fileName)
        {
            var localPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(localPath, "TestData", fileName);
        }
    }
}
