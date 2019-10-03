using Amazon.Route53;
using Amazon.Route53.Model;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
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
            try
            {
                await RunAsync(new LEServerManager
                {
                    Domains = new[] { "d.com" }
                });
            }catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }

        private static async Task RunAsync(LEServerManager serverManager)
        {

            var client = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
            

            var account = await client.NewAccount("d@d.com", true);

            var order = await client.NewOrder(new[] { "*.d.com", "d.com"});

            var list = new List<string>();

            var challenges = new List<IChallengeContext>();

            foreach(var auth in await order.Authorizations())
            {
                var dns = await auth.Dns();

                challenges.Add(dns);

                var txt = client.AccountKey.DnsTxt(dns.Token);
                list.Add(txt);
            }

            await UpdateRoutes("d.com", list);

            var tasks = challenges.Select(x => x.Validate()).ToList();

            await Task.WhenAll(tasks);

            var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);

            var csrBuilder = await order.CreateCsr(privateKey);

            csrBuilder.AddName("C=CA, ST=State, L=City, O=Dept, CN=d.com");
            csrBuilder.SubjectAlternativeNames = new List<string> { "*.d.com" };

            await order.Finalize(csrBuilder.Generate());

            var cert = await order.Download();
            
           
            //var cert = await order.Generate(new CsrInfo {
            //    CountryName = "US",
            //    State = "Florida",
            //    Locality = "Fort Pierce",
            //    Organization = "800 Software Systems",
            //    OrganizationUnit = "Web",
            //    CommonName = "all.800casting.com",
                
            //}, privateKey);

            var certPem = cert.ToPem();

            var pfxBuilder = cert.ToPfx(privateKey);

            var pfx = pfxBuilder.Build("all cert", "abcd123");

            await System.IO.File.WriteAllBytesAsync("d:\\temp\\a.pfx", pfx);
            Console.WriteLine("Certificate generated");
            Console.ReadLine();
            

        }

        private static async Task UpdateRoutes(string domain, IEnumerable<string> challenges)
        {
            using (var route53 = new AmazonRoute53Client(
                new Amazon.Runtime.BasicAWSCredentials("...", "..."),
                Amazon.RegionEndpoint.USEast1))
            {
                ChangeResourceRecordSetsRequest req = new ChangeResourceRecordSetsRequest
                {
                    HostedZoneId = "...",
                    ChangeBatch = new ChangeBatch()
                };
                req.ChangeBatch.Changes.Add(new Change
                {
                    Action = ChangeAction.UPSERT,
                    ResourceRecordSet = new ResourceRecordSet
                    {
                        Type = RRType.TXT,
                        TTL = 60,
                        Name = $"_acme-challenge.{domain}.8ct.co",
                        ResourceRecords = challenges.Select(x => new ResourceRecord($"\"{x}\"")).ToList()
                    }
                });
                var rs = await route53.ChangeResourceRecordSetsAsync(req);

                do
                {
                    await Task.Delay(1000);
                    var rc = await route53.GetChangeAsync(new GetChangeRequest { Id = rs.ChangeInfo.Id });
                    if (rc.ChangeInfo.Status == ChangeStatus.INSYNC)
                    {
                        break;
                    }
                } while (true);
            }

        }

        //private static async Task<List<LESite>> GetApprovedSites(LEServerManager serverManager, AcmeClient client)
        //{
        //    List<LESite> approvedSites = new List<LESite>();

        //    foreach (var site in serverManager.Sites)
        //    {
        //        foreach (var host in site.Hosts)
        //        {
        //            var auth = await client.NewAuthorization(new AuthorizationIdentifier
        //            {
        //                Type = "http-01",
        //                Value = host
        //            });

        //            var httpChallengeInfo = auth.Data.Challenges.First(c => c.Type == ChallengeTypes.Http01);
        //            string code = client.ComputeKeyAuthorization(httpChallengeInfo);
        //            string fileName = httpChallengeInfo.Token;

        //            var root = site.PhysicalPath;

        //            string filePath = $"{root}\\.well-known\\acme-challenge\\{fileName}";

        //            FileInfo file = new FileInfo(filePath);
        //            if (!file.Directory.Exists)
        //                file.Directory.Create();

        //            System.IO.File.WriteAllText(filePath, code);

        //            var a = await client.CompleteChallenge(httpChallengeInfo);

        //            var ca = await client.GetAuthorization(a.Location);

        //            while (ca.Data.Status == EntityStatus.Pending)
        //            {
        //                await Task.Delay(1000);
        //                ca = await client.GetAuthorization(a.Location);
        //            }

        //            if (ca.Data.Status == EntityStatus.Valid)
        //            {
        //                approvedSites.Add(site);
        //            }
        //            else
        //            {
        //                Console.WriteLine(ca.Data.Status);
        //                Console.WriteLine(ca.Raw);
        //            }
        //        }
        //    }

        //    return approvedSites;
        //}
    }
}
