using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SingleInstanceHelper
{
    public class NamedPipeXmlPayload
    {
        /// <summary>
        ///     A list of command line arguments.
        /// </summary>
        [XmlElement("CommandLineArguments")]
        public List<string> CommandLineArguments { get; set; } = new List<string>();
    }
}
