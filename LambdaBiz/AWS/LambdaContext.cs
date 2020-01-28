using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz.AWS
{
	public class LambdaContext : IServerlessContext
	{
		private AmazonLambdaClient _lambdaClient;
		public LambdaContext(string awsAccessKey,string awsSecretAccessKey, string awsRegion)
		{
			_lambdaClient = new AmazonLambdaClient(awsAccessKey, awsSecretAccessKey, RegionEndpoint.GetBySystemName(awsRegion));
		}

		public T CallTask<T>(string functionName, object input)
		{
			var result = CallTaskAsync<T>(functionName, input);
			result.Wait();
			return result.Result;
		}

		public object CallTask(string functionName, object input)
		{
			var result = CallTaskAsync(functionName, input);
			result.Wait();
			return result.Result;
		}

		public async Task<T> CallTaskAsync<T>(string functionName, object input)
		{
			var lambdaResponse = await CallLambdaAsync(functionName, input);

			var stream = new StreamReader(lambdaResponse.Payload);
			var jsonreader = new JsonTextReader(stream);
			var serializer = new JsonSerializer();

			return serializer.Deserialize<T>(jsonreader);
		}

		public async Task<object> CallTaskAsync(string functionName, object input)
		{
			var lambdaResponse = await CallLambdaAsync(functionName, input);

			IFormatter formatter = new BinaryFormatter();
			lambdaResponse.Payload.Seek(0, SeekOrigin.Begin);
			return formatter.Deserialize(lambdaResponse.Payload);
		}

		private async Task<InvokeResponse> CallLambdaAsync(string functionName, object input)
		{
			var payLoad = JsonConvert.SerializeObject(input);

			var invokeRequest = new InvokeRequest
			{
				FunctionName = functionName,
				InvocationType = InvocationType.RequestResponse,
				Payload = payLoad
			};

			return await _lambdaClient.InvokeAsync(invokeRequest);
		}
	}
}
