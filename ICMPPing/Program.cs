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
        static void Main(string[] args)
        {
            string address = "cs.wikipedia.org";
            int seconds = 60;

            Console.WriteLine($"Ping na adresu: {address} po dobu {seconds} sekund v intervalu 100ms");

            DateTime endTime = DateTime.Now.AddSeconds(seconds);
            string fileName = "PingResults.xml";

            using (var writer = XmlWriter.Create(fileName, new XmlWriterSettings() { Indent = true }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("PingResults");

                PerformPingTest(address, endTime, writer);

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

        static void PerformPingTest(string ipAddress, DateTime endTime, XmlWriter writer)
        {
            while (DateTime.Now < endTime)
            {
                PingReply reply = new Ping().Send(ipAddress, 300);

                //Console.WriteLine($"Ping odpověď z {reply.Address}: Odpověď={reply.Status}, Čas={reply.RoundtripTime}ms");
                writer.WriteStartElement("PingResult");
                writer.WriteElementString("IPAddress", ipAddress);
                writer.WriteElementString("Status", reply.Status.ToString());
                writer.WriteElementString("RoundtripTime", reply.RoundtripTime.ToString());
                writer.WriteEndElement();

                int delay = Math.Max(100, 300 - (int)reply.RoundtripTime);
                Thread.Sleep(delay);
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
