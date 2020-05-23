using System;
using Newtonsoft.Json;

namespace IoTHttpLoader
{
	public class HttpMessage
	{

        [JsonProperty("Data")]
		public string Data { get; set; }

	}
}
