using Certes;
using Certes.Acme;
using Certes.Pkcs;
using Microsoft.Web.Administration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LetsEncryptForIISClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //string fileContent = System.IO.File.ReadAllText(args[0]);

            //LEServerManager serverManager = JsonConvert.DeserializeObject<LEServerManager>(fileContent);

            //RunAsync(serverManager).Wait();

            RunAsync().Wait();
        }

        private static async Task RunAsync()
        {
            using (ServerManager serverManager = new ServerManager()){
                var s = new LEServerManager();

                s.Sites = serverManager.Sites.Select(x => new LESite {
                    Hosts = x.Bindings.Where(b=> !string.IsNullOrWhiteSpace(b.Host)).Select(b=>b.Host).ToList(),
                    PhysicalPath = x.Applications.First(a=>a.Path == "/").VirtualDirectories.FirstOrDefault().PhysicalPath
                });

                await RunAsync(s);
            }
        }

        private static async Task RunAsync(LEServerManager serverManager)
        {

            

            using (var client = new AcmeClient(WellKnownServers.LetsEncryptStaging))
            {

                var account = await client.NewRegistraton("mailto:ackava@gmail.com");

                account.Data.Agreement = account.GetTermsOfServiceUri();
                account = await client.UpdateRegistration(account);

                List<LESite> approvedSites = await GetApprovedSites(serverManager, client);

                var csr = new CertificationRequestBuilder();
                csr.AddName("CN", "all.letsenc.800casting.com");

                foreach (var site in approvedSites.SelectMany(a => a.Hosts))
                {
                    csr.SubjectAlternativeNames.Add(site);
                }

                var cert = await client.NewCertificate(csr);

                var pfxBuilder = cert.ToPfx();

                var pfx = pfxBuilder.Build("lets-encrypt-cert", "abcd123");
                await File.WriteAllBytesAsync("./lets-encrypt-cert.pfx", pfx);



            }

        }

        private static async Task<List<LESite>> GetApprovedSites(LEServerManager serverManager, AcmeClient client)
        {
            List<LESite> approvedSites = new List<LESite>();

            foreach (var site in serverManager.Sites)
            {
                foreach (var host in site.Hosts)
                {
                    var auth = await client.NewAuthorization(new AuthorizationIdentifier
                    {
                        Type = "http-01",
                        Value = host
                    });

                    var httpChallengeInfo = auth.Data.Challenges.First(c => c.Type == ChallengeTypes.Http01);
                    string code = client.ComputeKeyAuthorization(httpChallengeInfo);
                    string fileName = httpChallengeInfo.Token;

                    var root = site.PhysicalPath;

                    string filePath = $"{root}\\.well-known\\acme-challenge\\{fileName}";

                    FileInfo file = new FileInfo(filePath);
                    if (!file.Directory.Exists)
                        file.Directory.Create();

                    System.IO.File.WriteAllText(filePath, code);

                    var a = await client.CompleteChallenge(httpChallengeInfo);

                    var ca = await client.GetAuthorization(a.Location);

                    while (ca.Data.Status == EntityStatus.Pending)
                    {
                        await Task.Delay(1000);
                        ca = await client.GetAuthorization(a.Location);
                    }

                    if (ca.Data.Status == EntityStatus.Valid)
                    {
                        approvedSites.Add(site);
                    }
                    else
                    {
                        Console.WriteLine(ca.Data.Status);
                        Console.WriteLine(ca.Raw);
                    }
                }
            }

            return approvedSites;
        }
    }
}
