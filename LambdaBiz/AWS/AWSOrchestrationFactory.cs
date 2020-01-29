using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz.AWS
{
	public class AWSOrchestrationFactory : IOrchestrationFactory
	{
		private string _awsAccessKey;
		private string _awsSecretAccessKey;
		private string _awsRegion;
		public AWSOrchestrationFactory(string awsAccessKey, string awsSecretAccessKey, string awsRegion)
		{
			_awsAccessKey = awsAccessKey;
			_awsSecretAccessKey = awsSecretAccessKey;
			_awsRegion = awsRegion;
		}
		public IOrchestration CreateOrchestration(string orchestrationId)
		{
			return new AWSOrchestration(_awsAccessKey, _awsSecretAccessKey, _awsRegion);
		}

		public IOrchestration CreateOrchestration(string orchestrationId, IServerlessContext context)
		{
			return CreateOrchestration(orchestrationId);
		}
	}
}
