using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz.REST
{
	public class RESTConnector
	{
		public static async Task<string> SendAsync(RESTConfig config)
		{
			return await SendAsync(config.Url, config.Method, config.QueryString, config.Body, config.Headers);
		}
		public static async Task<string> SendAsync(string url, string method, string queryString,object input, Dictionary<string,string> headers)
		{
			
			var client = new HttpClient();
			client.BaseAddress = new Uri(url);

			if (headers != null)
			{
				foreach (var header in headers)
					client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
			}
			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            HttpContent content = null;

            if(input != null)
                content = new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json");

			HttpResponseMessage response = null;

			if (method.ToLower() == "get")
			{
				response = await client.GetAsync(queryString);
			}
			else if (method.ToLower() == "put")
			{
				response = await client.PutAsync(queryString, content);
			}
			else if (method.ToLower() == "post")
			{
				response = await client.PostAsync(queryString, content);
			}
			else if (method.ToLower() == "delete")
			{
				response = await client.DeleteAsync(queryString);
			}
			else
				throw new Exception("Method : " + method + " not implemented in RESTConnector.");

			
			if (response.IsSuccessStatusCode)
			{
				return await response.Content.ReadAsStringAsync();
			}
			else
				throw new Exception("Response from RESTService : " + response.StatusCode.ToString() + " : " + response.ReasonPhrase);
			
		}
	}
}
