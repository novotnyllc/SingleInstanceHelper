using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SingleInstanceHelper
{
    /// <summary>
    /// Helper for creating or passing command args to a single application instance
    /// </summary>
    public static class ApplicationActivator
    {
        private static readonly string DefaultUniqueName = GetRunningProcessHash();

        private static string GetMutexName(string uniqueName) =>
            $@"Mutex_{Environment.UserDomainName}_{Environment.UserName}_{uniqueName}";

        private static string GetPipeName(string uniqueName) =>
            $@"Pipe_{Environment.UserDomainName}_{Environment.UserName}_{uniqueName}";

        /// <summary>
        /// Determines if the application should continue launching or return because it's not the first instance.
        /// When not the first instance, the command line args will be passed to the first one.
        /// </summary>
        /// <param name="otherInstanceCallback">Callback to execute on the first instance with command line args
        /// from subsequent launches. If specified, it will run on the current synchronization context.</param>
        /// <param name="uniqueName">A unique identifier of the app running. Calling this method from
        /// many different apps with the same <paramref name="uniqueName"/> specified will return
        /// <see langword="true"/> only on one of them</param>
        /// <returns>Whether this is the first instance of the application that
        /// called this method.</returns>
        public static async Task<bool> LaunchOrReturnAsync(
            Action<IReadOnlyList<string>>? otherInstanceCallback = null, string? uniqueName = null)
        {
            var un = uniqueName ?? DefaultUniqueName;
            if (IsApplicationFirstInstance(un))
            {
                // Setup Named Pipe listener
                if (otherInstanceCallback != null)
#pragma warning disable 4014
                    Task.Run(async () => await CreateNamedPipeServer(un, otherInstanceCallback)).ConfigureAwait(false);
#pragma warning restore 4014
                return true;
            }

            // We are not the first instance, send the named pipe message with our payload and stop loading
            var namedPipeXmlPayload =
                new Payload {CommandLineArguments = Environment.GetCommandLineArgs().ToList()};

            // Send the message
            await SendOptionsToNamedPipe(un, namedPipeXmlPayload);
            return false; // Signal to quit
        }

        private static bool IsApplicationFirstInstance(string uniqueName)
        {
            var mutex = new Mutex(true, GetMutexName(uniqueName), out var isFirstInstance);
            return isFirstInstance;
        }

        private static async Task SendOptionsToNamedPipe(string uniqueName, Payload namedPipePayload)
        {
            using var pipeClient = new NamedPipeClientStream(".", GetPipeName(uniqueName), PipeDirection.Out);
            await pipeClient.ConnectAsync(3000); // Maximum wait 3 seconds

            if (pipeClient.IsConnected) await JsonSerializer.SerializeAsync(pipeClient, namedPipePayload);
        }

        private static async Task CreateNamedPipeServer(string uniqueName,
            Action<IReadOnlyList<string>> otherInstanceCallback)
        {
            var pipeName = GetPipeName(uniqueName);
            while (true)
            {
                // Create pipe and start the async connection wait
                using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                // Async wait for connections
                await pipeServer.WaitForConnectionAsync();
                if (!pipeServer.IsConnected) return;

                var payload = await JsonSerializer.DeserializeAsync<Payload>(pipeServer);

                var syncCtx = SynchronizationContext.Current;

                // payload contains the data sent from the other instance
                if (syncCtx != null)
                    syncCtx.Post(_ => otherInstanceCallback(payload.CommandLineArguments), null);
                else
                    otherInstanceCallback(payload.CommandLineArguments);
            }
        }

        private static string GetRunningProcessHash()
        {
            using var hash = SHA256.Create();
            var processPath = Assembly.GetEntryAssembly()!.Location;
            var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(processPath));
            return Convert.ToBase64String(bytes);
        }
    }
}
