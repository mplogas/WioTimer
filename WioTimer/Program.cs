using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WioTimer
{
    class Program
    {
        static void Main(string[] args)
        {
            var logic = new Logic(new HttpClient());
            logic.Connect();

            var input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input) && input.Equals("stop", StringComparison.InvariantCultureIgnoreCase))
            {
                logic.Dispose();
                return;
            }
        }
        
    }
}
