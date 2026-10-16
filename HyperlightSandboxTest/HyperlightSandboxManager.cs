using HyperlightSandbox.Api;
using HyperlightSandbox.Guest.JavaScript;
using HyperlightSandbox.Guest.Python;

namespace HyperlightSandboxTest
{
    // DTO for typed tools
    record MathArgs(double a, double b);

    internal class HyperlightSandboxManager
    {
        public void TestSandbox()
        {
            using var sandbox = new SandboxBuilder()
                                     .WithPythonModule()
                                     .Build();

            sandbox.RegisterTool<MathArgs, double>("add", args => args.a + args.b);
            sandbox.AllowDomain("https://httpbin.org");

            var result = sandbox.Run("""
                total = call_tool("add", a=3, b=4)
                resp = http_get("https://httpbin.org/get")
                print(f"3 + 4 = {total}, HTTP status: {resp['status']}")
                """);
            Console.WriteLine(result.Stdout);
        }

        /// <summary>
        /// Basic Usage
        /// </summary>
        public void TestPythonSandbox1()
        {
            // Create a sandbox with the Python guest
            using var sandbox = new SandboxBuilder()
                .WithPythonModule()
                .Build();

            // Execute code
            var result = sandbox.Run("""
                import math
                primes = [n for n in range(2, 50)
                          if all(n % i != 0 for i in range(2, int(math.sqrt(n)) + 1))]
                print(f"Primes: {primes}")
                """);

            Console.WriteLine(result.Stdout);   // Primes: [2, 3, 5, 7, 11, ...]
            Console.WriteLine(result.Success);  // True
        }


        public void TestPythonSandbox2()
        {
            using var sandbox = new SandboxBuilder()
                                     .WithPythonModule()
                                     .Build();
            var result = sandbox.Run("""
                def add(a, b):
                    return a + b
                total = add(3, 4)
                print(f"3 + 4 = {total}")
                """);
            Console.WriteLine(result.Stdout);

        }

        /// <summary>
        /// Tool Registration
        /// </summary>
        public void TestTool()
        {
            using var sandbox = new SandboxBuilder()
                                    .WithPythonModule()
                                    .Build();

            // Typed tool — auto-serializes args and result
            sandbox.RegisterTool<MathArgs, double>("add", args => args.a + args.b);

            // Raw JSON tool
            sandbox.RegisterTool("lookup", (string json) =>
                json.Contains("weather")
                    ? """{"temp": 22, "condition": "sunny"}"""
                    : """{"error": "unknown"}""");

            ExecutionResult result = sandbox.Run("""
                    sum = call_tool("add", a=10, b=32)
                    print(f"10 + 32 = {sum}")

                    weather = call_tool("lookup", key="weather")
                    print(f"Weather: {weather}")
                    """);

            Console.WriteLine(result.Stdout);
        }

        public void TestJavaScript1()
        {
            // Use the built-in QuickJS runtime — no guest module needed
            using var sandbox = new SandboxBuilder()
                                        .WithBackend(SandboxBackend.JavaScript)
                                        .Build();

            var result = sandbox.Run("console.log('Hello from JS!');");
            Console.WriteLine(result.Stdout);
        }

        public void TestJavaScript2()
        {
            // To run JavaScript through the Wasm guest module
            using var sandbox = new SandboxBuilder()
                                        .WithJavaScriptModule()
                                        .Build();

            var result = sandbox.Run("console.log('Hello from JS!');");
            Console.WriteLine(result.Stdout);
        }

        public void TestSnapshotRestore()
        {
            using var sandbox = new SandboxBuilder()
                                        .WithPythonModule()
                                        .Build();

            // Cold start (~2.5s)
            var result = sandbox.Run("pass");
            Console.WriteLine(result.Stdout);

            // Take snapshot of clean state
            using var snapshot = sandbox.Snapshot();

            // Execute code (modifies state)
            result = sandbox.Run("x = 42");
            Console.WriteLine(result.Stdout);

            // Restore to clean state (~2ms — 1000x faster than cold start)
            sandbox.Restore(snapshot);
            
            result = sandbox.Run("print(x)");  // NameError: x is not defined
            Console.WriteLine(result.Stdout);
        }

        public void TestPythonSandboxWithTool()
        {
            using var sandbox = new SandboxBuilder()
                                     .WithPythonModule()
                                     .Build();
            sandbox.RegisterTool<MathArgs, double>("add", args => args.a + args.b);
            var result = sandbox.Run("""
                total = call_tool("add", a=3, b=4)
                print(f"3 + 4 = {total}")
                """);
            Console.WriteLine(result.Stdout);

        }

        public void TestPythonSandboxWithHttp()
        {
            using var sandbox = new SandboxBuilder()
                                     .WithPythonModule()
                                     .Build();
            sandbox.AllowDomain("https://httpbin.org");
            var result = sandbox.Run("""
                import requests
                resp = requests.get("https://httpbin.org/get")
                print(f"HTTP status: {resp.status_code}")
                """);
            Console.WriteLine(result.Stdout);
        }

        public void TestPythonSandboxWithToolAndHttp()
        {
            using var sandbox = new SandboxBuilder()
                                     .WithPythonModule()
                                     .Build();
            sandbox.RegisterTool<MathArgs, double>("add", args => args.a + args.b);
            sandbox.AllowDomain("https://httpbin.org");
            var result = sandbox.Run("""
                total = call_tool("add", a=3, b=4)
                import requests
                resp = requests.get("https://httpbin.org/get")
                print(f"3 + 4 = {total}, HTTP status: {resp.status_code}")
                """);
            Console.WriteLine(result.Stdout);


        }


        //public void TestPythonSandboxWithToolAndHttpAndFile()
        //{
        //    using var sandbox = new SandboxBuilder()
        //                             .WithPythonModule()
        //                             .Build();
        //    sandbox.RegisterTool<MathArgs, double>("add", args => args.a + args.b);
        //    sandbox.AllowDomain("https://httpbin.org");
        //    sandbox.WriteFile("/tmp/test.txt", "Hello, world!");
        //    var result = sandbox.Run("""
        //        total = call_tool("add", a=3, b=4)
        //        import requests
        //        resp = requests.get("https://httpbin.org/get")
        //        with open("/tmp/test.txt", "r") as f:
        //            content = f.read()
        //        print(f"3 + 4 = {total}, HTTP status: {resp.status_code}, file content: {content}")
        //        """);
        //    Console.WriteLine(result.Stdout);
        //}


        //public void TestPythonSandboxWithToolAndHttpAndFileAndSnapshot()
        //{
        //    using var sandbox = new SandboxBuilder()
        //                             .WithPythonModule()
        //                             .Build();
        //    sandbox.RegisterTool<MathArgs, double>("add", args => args.a + args.b);
        //    sandbox.AllowDomain("https://httpbin.org");
        //    sandbox.WriteFile("/tmp/test.txt", "Hello, world!");
        //    var snapshot = sandbox.CreateSnapshot();
        //    var result = sandbox.Run("""
        //        total = call_tool("add", a=3, b=4)
        //        import requests
        //        resp = requests.get("https://httpbin.org/get")
        //        with open("/tmp/test.txt", "r") as f:
        //            content = f.read()
        //        print(f"3 + 4 = {total}, HTTP status: {resp.status_code}, file content: {content}")
        //        """);
        //    Console.WriteLine(result.Stdout);
        //    // Restore the snapshot and run again
        //    using var restoredSandbox = new SandboxBuilder()
        //                                     .WithPythonModule()
        //                                     .Build(snapshot);
        //    var restoredResult = restoredSandbox.Run("""
        //        total = call_tool("add", a=5, b=6)
        //        import requests
        //        resp = requests.get("https://httpbin.org/get")
        //        with open("/tmp/test.txt", "r") as f:
        //            content = f.read()
        //        print(f"5 + 6 = {total}, HTTP status: {resp.status_code}, file content: {content}")
        //        """);
        //    Console.WriteLine(restoredResult.Stdout);

        //}
    }
}
