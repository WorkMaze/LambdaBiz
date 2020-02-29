using Amazon;
using Amazon.DynamoDBv2;
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
        private string _lambdaRole;
        private AWSPeristantStore _store;
        public AWSOrchestrationFactory(string awsAccessKey, string awsSecretAccessKey, string awsRegion, bool usePersistantStore,string lambdaRole)
		{
			_amazonSimpleWorkflowClient = new AmazonSimpleWorkflowClient(awsAccessKey, awsSecretAccessKey, RegionEndpoint.GetBySystemName(awsRegion));
            _lambdaRole = lambdaRole;
            if (usePersistantStore)
                _store = new AWSPeristantStore(awsAccessKey, awsSecretAccessKey, awsRegion);
        }

        public AWSOrchestrationFactory( bool usePersistantStore, string lambdaRole)
        {
            _amazonSimpleWorkflowClient = new AmazonSimpleWorkflowClient();
            _lambdaRole = lambdaRole;
            if (usePersistantStore)
                _store = new AWSPeristantStore( );
        }

        /// <summary>
        /// Create orchestartion
        /// </summary>
        /// <param name="orchestrationId">Unique id to identify the orchestration</param>
        /// <returns></returns>
		public async Task<IOrchestration> CreateOrchestrationAsync(string orchestrationId)
		{
			try
			{
				await _amazonSimpleWorkflowClient.RegisterDomainAsync(new RegisterDomainRequest
				{
					Name = Constants.LAMBDA_BIZ_DOMAIN,
					WorkflowExecutionRetentionPeriodInDays = "90"
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

			try
			{
				await _amazonSimpleWorkflowClient.RegisterActivityTypeAsync(new RegisterActivityTypeRequest
				{
					Domain = Constants.LAMBDA_BIZ_DOMAIN,
					Name = Constants.LAMBDA_BIZ_ACTIVITY_TYPE,
					Version = Constants.LAMBDA_BIZ_TYPE_VERSION
				});
			}
			catch (TypeAlreadyExistsException)
			{

			}

            return new AWSOrchestration(_amazonSimpleWorkflowClient, orchestrationId, _store, _lambdaRole);
		}
				

	}
}
