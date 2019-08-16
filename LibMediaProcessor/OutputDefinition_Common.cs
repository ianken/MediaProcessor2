using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LibMediaProcessor
{
    public class OutputDefinition
    {
        public enum StreamRole
        {
            Undefined = 0,
            Streaming,
            Download,
            Both
        }

   
        public string OutputFileName { get; set; }
        public StreamRole Role { get; set; }
        public string StreamName { get; set; }

       
    }
}