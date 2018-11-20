using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace SingleInstanceHelper
{
    [DataContract]
    internal class Payload
    {
        /// <summary>
        ///     A list of command line arguments.
        /// </summary>
        [DataMember]
        public List<string> CommandLineArguments { get; set; } = new List<string>();
    }
}
