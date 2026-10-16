using HyperlightSandbox.Api;
using HyperlightSandbox.Guest.Python;

namespace HyperlightSandboxTest
{
    // https://github.com/hyperlight-dev/hyperlight-sandbox/blob/main/README.md
    // dotnet add package Hyperlight.HyperlightSandbox.Api
    // dotnet add package Hyperlight.HyperlightSandbox.Guest.Python

    // Doc
    // https://github.com/hyperlight-dev/hyperlight-sandbox/blob/main/src/sdk/dotnet/README.md
    // A .NET 8.0 SDK for running code in secure, sandboxed environments using hyperlight.
    // Execute Python, JavaScript, or custom guest code inside lightweight micro-VMs
    // with tool dispatch, filesystem isolation, network allowlists, and snapshot/restore.

    // Examples
    // https://github.com/hyperlight-dev/hyperlight-sandbox/tree/main/src/sdk/dotnet/core/Examples
    // https://github.com/hyperlight-dev/hyperlight-sandbox/tree/main/examples/agent-framework
    // https://github.com/hyperlight-dev/hyperlight-sandbox/tree/main/examples/copilot-sdk

    internal class Program
    {
        static void Main(string[] args)
        {
            var manager = new HyperlightSandboxManager();
            manager.TestTool1();
            //manager.TestTool2();

            //manager.TestPythonSandbox1();
            //manager.TestJavaScript1();
            //manager.TestJavaScript2();
            //manager.TestSnapshotRestore();
            // manager.TestFilesystemAccess();
            //manager.TestNetworkAllowlist();
        }
    }
}
