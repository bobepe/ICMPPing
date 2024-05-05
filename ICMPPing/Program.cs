using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ICMPPing
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            List<string> ipAddresses = new List<string>
            {
                "seznam.cz",
                "google.cz",
                "cs.wikipedia.org",
                "youtube.com",
                "bing.com"
            };

            int seconds = 60;

            DateTime endTime = DateTime.Now.AddSeconds(seconds);
            string fileName = "PingResults.xml";

            using (var writer = XmlWriter.Create(fileName, new XmlWriterSettings() { Indent = true }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("PingResults");

                var tasks = new List<Task>();

                foreach (string ipAddress in ipAddresses)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await PerformPingTest(ipAddress, endTime, writer);
                    }));
                }

                await Task.WhenAll(tasks);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            Console.WriteLine("Test dokončen.");
            Dictionary<string, AvailabilityAddresses> availabilityPercentages = ReadAvailability(fileName);
            Console.WriteLine("Výsledky testu:");
            foreach (var kvp in availabilityPercentages)
            {
                Console.WriteLine($"IP adresa: {kvp.Key}, Dostupnost: {kvp.Value.AvailabilityPercentages:F2}%");
            }
        }

        static async Task PerformPingTest(string ipAddress, DateTime endTime, XmlWriter writer)
        {
            while (DateTime.Now < endTime)
            {
                PingReply reply = await new Ping().SendPingAsync(ipAddress, 300);

                lock (writer)
                {
                    writer.WriteStartElement("PingResult");
                    writer.WriteElementString("IPAddress", ipAddress);
                    writer.WriteElementString("Status", reply.Status.ToString());
                    writer.WriteElementString("RoundtripTime", reply.RoundtripTime.ToString());
                    writer.WriteEndElement();
                }

                int delay = 100;
                int response = (int)reply.RoundtripTime;
                if (reply.Status == IPStatus.Success && (delay - response) > 0) delay -= response;
                else delay = 300;
                await Task.Delay(delay);
            }
        }

        static Dictionary<string, AvailabilityAddresses> ReadAvailability(string fileName)
        {
            var availabilityPercentages = new Dictionary<string, AvailabilityAddresses>();

            try
            {
                using (XmlReader reader = XmlReader.Create(fileName))
                {
                    string ipAddress = null;

                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "IPAddress")
                        {
                            ipAddress = reader.ReadElementContentAsString();
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Status")
                        {
                            if (!string.IsNullOrEmpty(ipAddress))
                            {
                                string status = reader.ReadElementContentAsString();
                                bool isSucces = status == "Success";

                                if (availabilityPercentages.ContainsKey(ipAddress))
                                {
                                    availabilityPercentages[ipAddress].Total += 1;
                                    if (isSucces)
                                    {
                                        availabilityPercentages[ipAddress].Succes += 1;
                                    }
                                }
                                else
                                {
                                    availabilityPercentages.Add(ipAddress, new AvailabilityAddresses() { Total = 1, Succes = isSucces ? 1 : 0 });
                                }

                                ipAddress = null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při čtení XML souboru: {ex.Message}");
            }

            return availabilityPercentages;
        }
    }
}
