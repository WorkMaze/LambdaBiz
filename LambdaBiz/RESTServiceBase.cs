using LambdaBiz.REST;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    public abstract class RESTServiceBase
    {
        protected abstract Task<RESTActivity> GetScheduledActivityAsync(string orchestrationId);
        protected abstract Task SucceedActivityAsync(string taskToken, string result);
        protected abstract Task FailActivityAsync(string taskToken, string error);
        public async Task Run(string orchestrationId)
        {
            var scheduledActivity = await GetScheduledActivityAsync(orchestrationId);

            try
            {
                var response = await RESTConnector.SendAsync(scheduledActivity.RESTConfig);

                await SucceedActivityAsync(scheduledActivity.TaskToken, response);
            }
            catch(Exception ex)
            {
                await FailActivityAsync(scheduledActivity.TaskToken, ex.Message);
            }

        }
    }
}
