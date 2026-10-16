using HyperlightSandbox.Api;
using HyperlightSandbox.Guest.Python;

namespace HyperlightSandboxTest
{
    // https://github.com/hyperlight-dev/hyperlight-sandbox/blob/main/README.md
    // dotnet add package Hyperlight.HyperlightSandbox.Api
    // dotnet add package Hyperlight.HyperlightSandbox.Guest.Python

    // https://github.com/hyperlight-dev/hyperlight-sandbox/blob/main/src/sdk/dotnet/README.md
    // A .NET 8.0 SDK for running code in secure, sandboxed environments using hyperlight.
    // Execute Python, JavaScript, or custom guest code inside lightweight micro-VMs
    // with tool dispatch, filesystem isolation, network allowlists, and snapshot/restore.

    internal class Program
    {
        static void Main(string[] args)
        {
            var manager = new HyperlightSandboxManager();
            //manager.TestPythonSandbox1();
            //manager.TestTool();
            //manager.TestJavaScript1();
            //manager.TestJavaScript2();
            manager.TestSnapshotRestore();
        }
    }
}
