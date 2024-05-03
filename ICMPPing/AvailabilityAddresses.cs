using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICMPPing
{
    public class AvailabilityAddresses
    {
        public int Total { get; set; }
        public int Succes { get; set; }
        public int AvailabilityPercentages
        {
            get
            {
                return Succes * 100 / Total;
            }
        }
    }
}
