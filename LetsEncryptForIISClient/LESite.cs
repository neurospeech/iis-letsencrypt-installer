using System.Collections.Generic;

namespace LetsEncryptForIISClient
{
    public class LESite
    {
        public string PhysicalPath { get; set; }
        public List<string> Hosts { get; set; }
    }
}