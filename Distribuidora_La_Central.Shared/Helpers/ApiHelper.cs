using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Distribuidora_La_Central.Shared.Helpers
{
    public  class ApiHelper
    {
        public static string GetApiUrl(string endpoint)
        {
            if (OperatingSystem.IsAndroid())
            {
                return $"http://10.0.2.2:5282/api/{endpoint}";
            }
            else if (OperatingSystem.IsBrowser())
            {
                return $"http://localhost:5282/api/{endpoint}";
            }
            else
            {
                // Si más adelante lo corrés en Windows/Mac nativo
                return $"http://localhost:5282/api/{endpoint}";
            }
        }
    }
}
