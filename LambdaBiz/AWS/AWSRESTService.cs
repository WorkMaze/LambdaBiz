using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using LambdaBiz.REST;
using Newtonsoft.Json;

namespace LambdaBiz.AWS
{
    public class AWSRESTService : RESTServiceBase
    {
        private AmazonSimpleWorkflowClient _amazonSimpleWorkflowClient;
        public AWSRESTService(string awsAccessKey, string awsSecretAccessKey, string awsRegion)
        {
            _amazonSimpleWorkflowClient = new AmazonSimpleWorkflowClient(awsAccessKey, awsSecretAccessKey, RegionEndpoint.GetBySystemName(awsRegion));

        }
        protected override async Task FailActivityAsync(string taskToken, string error)
        {
            var activityFailedRequest = new RespondActivityTaskFailedRequest
            {
                TaskToken = taskToken,
                Details = error,
                Reason = error
            };

            await _amazonSimpleWorkflowClient.RespondActivityTaskFailedAsync(activityFailedRequest);
        }

        protected override async Task<RESTActivity> GetScheduledActivityAsync(string orchestrationId)
        {
            RESTActivity activity = null;

            var activityTaskRequest = new PollForActivityTaskRequest
            {
                Domain = Constants.LAMBDA_BIZ_DOMAIN,
                Identity = Guid.NewGuid().ToString(),
                TaskList = new TaskList
                {
                    Name = Constants.LAMBDA_BIZ_TASK_LIST + orchestrationId
                }
            };

            var activityTaskResponse = await _amazonSimpleWorkflowClient.PollForActivityTaskAsync(activityTaskRequest);
            
            if (activityTaskResponse.ActivityTask.TaskToken != null)
            {
                activity = new RESTActivity
                {
                    TaskToken = activityTaskResponse.ActivityTask.TaskToken,
                    RESTConfig = JsonConvert.DeserializeObject<RESTConfig>(activityTaskResponse.ActivityTask.Input)
                };
            }

            return activity;
        }

        protected override async Task SucceedActivityAsync(string taskToken, string result)
        {
            var activityCompletedRequest = new RespondActivityTaskCompletedRequest
            {
                TaskToken = taskToken,
                Result = result
            };

            await _amazonSimpleWorkflowClient.RespondActivityTaskCompletedAsync(activityCompletedRequest);
        }
    }
}
