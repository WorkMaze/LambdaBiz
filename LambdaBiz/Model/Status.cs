using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace LambdaBiz.Model
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Status
    {
        NONE,STARTED, SUCCEEDED, FAILED, TIMEOUT,  SCHEDULED
    }
}
