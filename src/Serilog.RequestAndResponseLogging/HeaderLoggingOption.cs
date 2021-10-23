using System;

namespace Serilog.RequestAndResponseLogging
{
    public class HeaderLoggingOption
    {
        /// <summary>
        /// Log all headers
        /// </summary>
        public bool LogAll { get; set; }

        /// <summary>
        /// Prefix for header like a prefix_headerKey 
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Headers for include
        /// </summary>
        public string[] Include { get; set; }

        /// <summary>
        /// Headers for exclude
        /// </summary>
        public string[] Exclude { get; set; }

        /// <summary>
        /// Load for default options
        /// </summary>
        public HeaderLoggingOption()
        {
            Include = Array.Empty<string>();
            Exclude = Array.Empty<string>();
        }
    }
}