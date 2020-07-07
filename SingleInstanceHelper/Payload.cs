using System.Collections.Generic;

namespace SingleInstanceHelper
{
    internal class Payload
    {
        /// <summary>
        ///     A list of command line arguments.
        /// </summary>
        public List<string> CommandLineArguments { get; set; } = new List<string>();
    }
}
