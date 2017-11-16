using System.Collections.Generic;

namespace LetsEncryptForIISClient
{
    internal class ServerManager
    {
        public IEnumerable<Site> Sites { get; internal set; }
    }
}