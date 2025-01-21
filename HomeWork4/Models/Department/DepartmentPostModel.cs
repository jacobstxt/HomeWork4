using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace _6.NovaPoshta.Models.Department
{
    public class DepartmentPostModel
    {
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; } = string.Empty;
        [JsonProperty("modelName")]
        public string ModelName { get; set; } = "Address";
        [JsonProperty("calledMethod")]
        public string CalledMethod { get; set; } = "getWarehouses";
        [JsonProperty("methodProperties")]
        public MethodDepartmentProperties? MethodProperties { get; set; }
    }

    public class MethodDepartmentProperties
    {
        public string CityRef { get; set; } = string.Empty;
    }

}
