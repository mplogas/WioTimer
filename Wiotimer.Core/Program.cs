using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NLog;
using WioLibrary;
using WioLibrary.Logging;

namespace Wiotimer.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = new LogWrapper();
            var http = new HttpClient();

            var l = new Logic(logger, http);
            l.Connect().Wait();

            var input = Console.ReadLine();
            if (!string.IsNullOrEmpty(input) && input.Equals("stop", StringComparison.CurrentCultureIgnoreCase))
            {
                l.Disconnect().Wait();
                return;
            }
        }

        
    }
}
