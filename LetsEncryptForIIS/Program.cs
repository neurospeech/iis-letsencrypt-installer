using Certes;
using Certes.Acme;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncryptForIIS
{
    class Program
    {
        static void Main(string[] args)
        {
            RunAsync().Wait();
        }



        static async Task RunAsync() {

            using (ServerManager serverManager = new ServerManager())
            {

                using (var client = new AcmeClient(WellKnownServers.LetsEncryptStaging))
                {

                    var account = await client.NewRegistraton("mailto:ackava@gmail.com");

                    account.Data.Agreement = account.GetTermsOfServiceUri();
                    account = await client.UpdateRegistration(account);

                    List<Site> approvedSites = new List<Site>();

                    foreach (var site in serverManager.Sites)
                    {
                        foreach (var host in site.Bindings.Where(x=>!string.IsNullOrWhiteSpace(x.Host)))
                        {
                            var auth = await client.NewAuthorization(new AuthorizationIdentifier
                            {
                                Type = "http-01",
                                Value = host.Host
                            });

                            var httpChallengeInfo = auth.Data.Challenges.First(c => c.Type == ChallengeTypes.Http01);
                            string code = client.ComputeKeyAuthorization(httpChallengeInfo);
                            string fileName = httpChallengeInfo.Token;

                            var root = site.Applications.Where(x => x.Path == "/").First().VirtualDirectories.First().PhysicalPath;

                            string filePath = $"{root}\\.well-known\\acme-challenge\\{fileName}";

                            System.IO.File.WriteAllText(filePath, code);

                            var a = await client.CompleteChallenge(httpChallengeInfo);

                            var ca = await client.GetAuthorization(a.Location);

                            while (ca.Data.Status == EntityStatus.Pending) {
                                await Task.Delay(1000);
                                ca = await client.GetAuthorization(a.Location);
                            }

                            if (ca.Data.Status == EntityStatus.Valid)
                            {
                                approvedSites.Add(site);
                            }
                            else {
                                Console.WriteLine(ca.Data.Status);
                                Console.WriteLine(ca.Raw);
                            }
                        }
                    }

                }
            }

        }

    }
}
