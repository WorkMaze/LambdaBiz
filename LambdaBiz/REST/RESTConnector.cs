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
			HttpContent content = new StringContent(JsonConvert.SerializeObject(input), Encoding.UTF8, "application/json");

			Task<HttpResponseMessage> response = null;

			if (method.ToLower() == "get")
			{
				response = client.GetAsync(queryString);
			}
			else if (method.ToLower() == "put")
			{
				response = client.PutAsync(queryString, content);
			}
			else if (method.ToLower() == "post")
			{
				response = client.PostAsync(queryString, content);
			}
			else if (method.ToLower() == "delete")
			{
				response = client.DeleteAsync(queryString);
			}
			else
				throw new Exception("Method : " + method + " not implemented in RESTConnector.");

			response.Wait();

			if (response.Result.IsSuccessStatusCode)
			{
				return await response.Result.Content.ReadAsStringAsync();
			}
			else
				throw new Exception("Response from RESTService : " + response.Result.StatusCode.ToString() + " : " + response.Result.ReasonPhrase);
			
		}
	}
}
