using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ICMPPing
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string address = "cs.wikipedia.org";
            int seconds = 60;

            Console.WriteLine($"Ping na adresu: {address} po dobu {seconds} sekund v intervalu 100ms");

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;

            byte[] buffer = new byte[32];
            int timeout = 300;

            DateTime endTime = DateTime.Now.AddSeconds(seconds);

            int successfulPings = 0;
            int totalPings = 0;
            while (DateTime.Now < endTime)
            {
                PingReply reply = pingSender.Send(address, timeout, buffer, options);
                //Console.WriteLine($"Ping odpověď z {reply.Address}: Odpověď={reply.Status}, Čas={reply.RoundtripTime}ms");
                if (reply.Status == IPStatus.Success)
                    successfulPings++;

                totalPings++;

                Thread.Sleep(100);
            }

            Console.WriteLine($"Dostupnost pro {address} je: {successfulPings * 100 / totalPings}%");
        }
    }
}
