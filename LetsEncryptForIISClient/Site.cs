using System.Collections.Generic;

namespace LetsEncryptForIISClient
{
    public class Site
    {
        public object PhysicalPath { get; set; }
        public List<string> Hosts { get; set; }
    }
}