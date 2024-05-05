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
            List<string> ipAddresses;
            int seconds = 60;

            if (args.Length < 2 || !int.TryParse(args[0], out seconds))
            {
                Console.WriteLine("Použití: ICMPPing.exe <doba v sekundách> <IP1> <IP2> ...");
                return;
            }

            ipAddresses = new List<string>();
            for (int i = 1; i < args.Length; i++)
            {
                ipAddresses.Add(args[i]);
            }

            DateTime endTime = DateTime.Now.AddSeconds(seconds);
            string fileName = "PingResults.xml";

            try
            {
                Console.WriteLine($"Ping po dobu {seconds} sekund v intervalu 100ms");
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

                Console.WriteLine("Test dokončen.\nVýsledky testu:");
                foreach (string ipAddr in ipAddresses)
                {
                    Console.WriteLine($"IP adresa: {ipAddr}, Dostupnost: {GetAvailability(fileName, ipAddr):F2}%");
                }
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} -> {ex.InnerException.Message}");
            }
        }

        /// <summary>
        /// Provede Ping na danou adresu a výsledek zapíše do souboru
        /// </summary>
        /// <param name="ipAddress">ip adresa</param>
        /// <param name="endTime">doba běhu</param>
        /// <param name="writer">XmlWriter</param>
        /// <returns></returns>
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

        /// <summary>
        /// Vypočte dostupnost pro danou ip adresu
        /// </summary>
        /// <param name="fileName">název souboru</param>
        /// <param name="ipAddr">ip adresa</param>
        /// <returns>Procento dostupnosti ip adresy</returns>
        static double GetAvailability(string fileName, string ipAddr)
        {
            int total = 0;
            int success = 0;

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
                            if (ipAddress != ipAddr) continue;
                        }
                        else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Status" && ipAddress == ipAddr)
                        {
                            if (!string.IsNullOrEmpty(ipAddress))
                            {
                                string status = reader.ReadElementContentAsString();
                                bool isSucces = status == "Success";

                                total++;
                                if (isSucces) success++;

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

            if (total >= success) return success * 100 / total;
            else return 0.0;
        }
    }
}
