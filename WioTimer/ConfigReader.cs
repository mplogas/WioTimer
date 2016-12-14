using System;
using System.Configuration;

namespace WioTimer
{
    public class ConfigReader
    {
        public static T GetValue<T>(string key)
        {
            var config = ConfigurationManager.AppSettings.Get(key);
            if (!string.IsNullOrEmpty(config))
            {
                return (T) Convert.ChangeType(config, typeof(T));
            }

            return default(T);
        } 
    }
}