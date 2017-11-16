using Certes;
using Certes.Acme;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LetsEncryptForIISClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileContent = System.IO.File.ReadAllText(args[0]);

            ServerManager serverManager = JsonConvert.DeserializeObject<ServerManager>(fileContent);

            RunAsync(serverManager).Wait();
        }

        private static async Task RunAsync(ServerManager serverManager)
        {

            

            using (var client = new AcmeClient(WellKnownServers.LetsEncryptStaging))
            {

                var account = await client.NewRegistraton("mailto:ackava@gmail.com");

                account.Data.Agreement = account.GetTermsOfServiceUri();
                account = await client.UpdateRegistration(account);

                List<Site> approvedSites = new List<Site>();

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

            }

        }
    }
}
