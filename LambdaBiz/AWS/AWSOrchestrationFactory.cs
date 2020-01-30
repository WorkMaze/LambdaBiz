using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz.AWS
{
	public class AWSOrchestrationFactory : IOrchestrationFactory
	{
		private AmazonSimpleWorkflowClient _amazonSimpleWorkflowClient;
		public AWSOrchestrationFactory(string awsAccessKey, string awsSecretAccessKey, string awsRegion)
		{
			_amazonSimpleWorkflowClient = new AmazonSimpleWorkflowClient(awsAccessKey, awsSecretAccessKey, RegionEndpoint.GetBySystemName(awsRegion));
		}
		public async Task<IOrchestration> CreateOrchestrationAsync(string orchestrationId)
		{
			try
			{
				await _amazonSimpleWorkflowClient.RegisterDomainAsync(new RegisterDomainRequest
				{
					Name = Constants.LAMBDA_BIZ_DOMAIN,
					WorkflowExecutionRetentionPeriodInDays = "0"
				});
			}
			catch(DomainAlreadyExistsException)
			{

			}

			try
			{
				await _amazonSimpleWorkflowClient.RegisterWorkflowTypeAsync(new RegisterWorkflowTypeRequest
				{
					Domain = Constants.LAMBDA_BIZ_DOMAIN,
					Name = Constants.LAMBDA_BIZ_WORKFLOW_TYPE,
					Version = Constants.LAMBDA_BIZ_TYPE_VERSION
				});
			}
			catch (TypeAlreadyExistsException)
			{

			}

			return new AWSOrchestration(_amazonSimpleWorkflowClient, orchestrationId);
		}
				

		public async Task<IOrchestration> CreateOrchestrationAsync(string orchestrationId, IServerlessContext context)
		{
			return await CreateOrchestrationAsync(orchestrationId);
		}
	}
}
