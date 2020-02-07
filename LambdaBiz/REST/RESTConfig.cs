using System;
using System.Collections.Generic;
using System.Text;

namespace LambdaBiz.REST
{
	internal class RESTConfig
	{
		public string Url { get; set; }
		public string Method { get; set; }
		public Dictionary<string, string> Headers { get; set; }
		public object Body { get; set; }
		public string QueryString { get; set; }
	}
}
