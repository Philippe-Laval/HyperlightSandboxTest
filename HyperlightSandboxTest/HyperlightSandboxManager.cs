using HyperlightSandbox.Api;
using HyperlightSandbox.Guest.JavaScript;
using HyperlightSandbox.Guest.Python;
using System.Text.Json.Serialization;

namespace HyperlightSandboxTest
{
    // DTO for typed tools
    internal sealed class MathArgs
    {
        [JsonPropertyName("a")]
        public double A { get; set; }

        [JsonPropertyName("b")]
        public double B { get; set; }
    }

    internal sealed class GreetArgs
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "world";
    }

    internal class HyperlightSandboxManager
    {
        public void TestTool1()
        {
            using var sandbox = new SandboxBuilder()
                                     .WithPythonModule()
                                     .Build();

            sandbox.RegisterTool<MathArgs, double>("add", args => args.A + args.B);
            sandbox.RegisterTool<MathArgs, double>("multiply", args => args.A * args.B);
            sandbox.RegisterTool<GreetArgs, string>("greet", args => $"Hello, {args.Name}!");

            // --- Register a raw JSON tool ---
            sandbox.RegisterTool("lookup", (string json) =>
            {
                // Simple key-value lookup.
                if (json.Contains("api_key"))
                    return """{"result": "sk-demo-12345"}""";
                if (json.Contains("model"))
                    return """{"result": "gpt-4"}""";
                return """{"result": "not found"}""";
            });

            // --- Register an async typed tool (e.g. simulating an external API call) ---
            sandbox.RegisterToolAsync<MathArgs, double>("add_async", async args =>
            {
                await Task.Delay(10).ConfigureAwait(false); // Simulate I/O latency
                return args.A + args.B;
            });


            sandbox.AllowDomain("https://httpbin.org");

            Console.WriteLine("═══ Test 1: Typed tool dispatch ═══");
            var result = sandbox.Run("""
    sum_result = call_tool("add", a=10, b=32)
    print(f"10 + 32 = {sum_result}")

    product = call_tool("multiply", a=7, b=6)
    print(f"7 × 6 = {product}")

    greeting = call_tool("greet", name="Hyperlight")
    print(f"Greeting: {greeting}")
    """);

            Console.WriteLine($"stdout:\n{result.Stdout}");

            // --- Test 2: Raw JSON tool dispatch ---
            Console.WriteLine("═══ Test 2: Raw JSON tool dispatch ═══");
            result = sandbox.Run("""
    key = call_tool("lookup", key="api_key")
    print(f"API key: {key}")

    model = call_tool("lookup", key="model")
    print(f"Model: {model}")
    """);

            Console.WriteLine($"stdout:\n{result.Stdout}");

            // --- Test 3: Async tool dispatch ---
            Console.WriteLine("═══ Test 3: Async tool dispatch ═══");

            result = sandbox.Run("""
    async_sum = call_tool("add_async", a=100, b=200)
    print(f"Async 100 + 200 = {async_sum}")
    """);

            Console.WriteLine($"stdout:\n{result.Stdout}");

            result = sandbox.Run("""
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
        public void TestTool2()
        {
            using var sandbox = new SandboxBuilder()
                                    .WithPythonModule()
                                    .Build();

            // Typed tool — auto-serializes args and result
            sandbox.RegisterTool<MathArgs, double>("add", args => args.A + args.B);

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

        public void TestFilesystemAccess()
        {
            using var sandbox = new SandboxBuilder()
                                        .WithPythonModule()
                                        .WithInputDir(@"C:\temp")    // Read-only /input in guest
                                        .WithTempOutput()            // Writable /output in guest
                                        .Build();

            var result = sandbox.Run("""
with open("/input/file1.txt") as f:
    data = f.read()
with open("/output/result.txt", "w") as f:
    f.write(data.upper())
""");
            Console.WriteLine(result.Stdout);

            var files = sandbox.GetOutputFiles();  // ["result.txt"]
            var path = sandbox.OutputPath;         // /tmp/hyperlight-xxx/
            // C:\Users\philippe.laval\AppData\Local\Temp\.tmpR90PO2
            Console.WriteLine(path);

            string fullPath = Path.Combine(path!, files[0]);
            string content = File.ReadAllText(fullPath);
            Console.WriteLine(content);
        }

        public void TestNetworkAllowlist()
        {
            using var sandbox = new SandboxBuilder()
                                        .WithPythonModule()
                                        .Build();

            sandbox.AllowDomain("https://httpbin.org");                    // All methods
            sandbox.AllowDomain("https://api.example.com", ["GET", "POST"]); // Filtered

            var result = sandbox.Run("""
    response = http_get("https://httpbin.org/get")
    print(f"response: {response}")
    print(f"Status: {response['status']}")
    print(f"Body: {response['body']}")
    """);
            // response: {'status': 200, 'body': '{\n  "args": {}, \n  "headers": {\n    "Host": "httpbin.org", \n    "User-Agent": "hyperlight-sandbox/0.1", \n    "X-Amzn-Trace-Id": "Root=1-6ad1e34a-4e5f58a8438d07672f819bc5"\n  }, \n  "origin": "109.210.38.57", \n  "url": "https://httpbin.org/get"\n}\n'}
            Console.WriteLine(result.Stdout);
        }

        public void TestPythonSandboxWithTool()
        {
            using var sandbox = new SandboxBuilder()
                                     .WithPythonModule()
                                     .Build();
            sandbox.RegisterTool<MathArgs, double>("add", args => args.A + args.B);
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
            sandbox.RegisterTool<MathArgs, double>("add", args => args.A + args.B);
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
