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
        /// <summary>
        /// Unique name to base the single instance decision on. Default's to a hash based on the executable location.
        /// </summary>
        public static string UniqueName { get; set; } = GetRunningProcessHash();

        private static Mutex _mutexApplication;
        private static readonly object _mutexLock = new object();
        private static bool _firstApplicationInstance;
        private static SynchronizationContext _syncContext;
        private static Action<IReadOnlyList<string>> _otherInstanceCallback;

        private static string GetMutexName() =>
            $@"Mutex_{Environment.UserDomainName}_{Environment.UserName}_{UniqueName}";

        private static string GetPipeName() =>
            $@"Pipe_{Environment.UserDomainName}_{Environment.UserName}_{UniqueName}";

        /// <summary>
        /// Determines if the application should continue launching or return because it's not the first instance.
        /// When not the first instance, the command line args will be passed to the first one.
        /// </summary>
        /// <param name="otherInstanceCallback">Callback to execute on the first instance with command line args
        /// from subsequent launches. Will run on the current synchronization context.</param>
        /// <returns>true if the first instance, false if it's not the first instance.</returns>
        public static async Task<bool> LaunchOrReturnAsync(Action<IReadOnlyList<string>> otherInstanceCallback)
        {
            _otherInstanceCallback =
                otherInstanceCallback ?? throw new ArgumentNullException(nameof(otherInstanceCallback));

            if (IsApplicationFirstInstance())
            {
                _syncContext = SynchronizationContext.Current;
                // Setup Named Pipe listener
#pragma warning disable 4014
                Task.Run(CreateNamedPipeServer).ConfigureAwait(false);
#pragma warning restore 4014
                return true;
            }
            else
            {
                // We are not the first instance, send the named pipe message with our payload and stop loading
                var namedPipeXmlPayload = new Payload
                {
                    CommandLineArguments = Environment.GetCommandLineArgs().ToList()
                };

                // Send the message
                await SendOptionsToNamedPipe(namedPipeXmlPayload);
                return false; // Signal to quit
            }
        }

        /// <summary>
        ///     Checks if this is the first instance of this application. Can be run multiple times.
        /// </summary>
        /// <returns></returns>
        private static bool IsApplicationFirstInstance()
        {
            if (_mutexApplication == null)
            {
                lock (_mutexLock)
                {
                    // Allow for multiple runs but only try and get the mutex once
                    if (_mutexApplication == null)
                    {
                        _mutexApplication = new Mutex(true, GetMutexName(), out _firstApplicationInstance);
                    }
                }
            }

            return _firstApplicationInstance;
        }

        /// <summary>
        ///     Uses a named pipe to send the currently parsed options to an already running instance.
        /// </summary>
        /// <param name="namedPipePayload"></param>
        private static async Task SendOptionsToNamedPipe(Payload namedPipePayload)
        {
            using var pipeClient = new NamedPipeClientStream(".", GetPipeName(), PipeDirection.Out);
            await pipeClient.ConnectAsync(3000); // Maximum wait 3 seconds

            if (pipeClient.IsConnected)
            {
                await JsonSerializer.SerializeAsync(pipeClient, namedPipePayload);
            }
        }

        /// <summary>
        ///     Starts a new pipe server if one isn't already active.
        /// </summary>
        private static async Task CreateNamedPipeServer()
        {
            while (true)
            {
                // Create pipe and start the async connection wait
                using var pipeServer = new NamedPipeServerStream(GetPipeName(), PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                // Async wait for connections
                await pipeServer.WaitForConnectionAsync();
                if (!pipeServer.IsConnected) return;

                var payload = await JsonSerializer.DeserializeAsync<Payload>(pipeServer);

                // payload contains the data sent from the other instance
                if (_syncContext != null)
                {
                    _syncContext.Post(_ => _otherInstanceCallback(payload.CommandLineArguments), null);
                }
                else
                {
                    _otherInstanceCallback(payload.CommandLineArguments);
                }
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
