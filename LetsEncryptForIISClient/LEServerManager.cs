using System.Collections.Generic;

namespace LetsEncryptForIISClient
{
    internal class LEServerManager
    {
        public IEnumerable<LESite> Sites { get; internal set; }
    }
}