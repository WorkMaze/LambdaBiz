using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LambdaBiz.AWS;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaBiz.Serverless
{
    public class Functions
    {
        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
        }
        public class Numbers
        {
            public int Number1 { get; set; }
            public int Number2 { get; set; }
        }

        public class Request
        {
            public string LambdaRole { get; set; }
            public Numbers Numbers { get; set; }
            public string OrchestrationId { get; set; }
        }
        public class OperationResult
        {
            public int Number1 { get; set; }
            public int Number2 { get; set; }
            public double Result { get; set; }
        }

        public async Task<Model.Workflow> ProcessAsync(Request request, ILambdaContext context)
        {
            /// Initialize orchestration factory for AWS
            var orchestrationFactory = new AWSOrchestrationFactory(true, request.LambdaRole);

            context.Logger.LogLine(JsonConvert.SerializeObject(request));
            /// Create a new .
            var orchestration = await orchestrationFactory.CreateOrchestrationAsync(request.OrchestrationId);
            context.Logger.LogLine("Created");
            try
            {

                /// Start workflow
                await orchestration.StartWorkflowAsync("Workflow Started");
                context.Logger.LogLine("Started");

                /// Call AWS lambda task
                var a = await orchestration.CallTaskAsync<Numbers>("Number", request.Numbers, "Operation1");
                context.Logger.LogLine("Operation1");

                var b = await orchestration.CallTaskAsync<OperationResult>("Sum", a, "Operation2");
                context.Logger.LogLine("Operation2");

                var c = await  orchestration.CallTaskAsync<OperationResult>("Difference", a, "Operation3");
                context.Logger.LogLine("Operation3");

                var d = await orchestration.CallTaskAsync<OperationResult>("Product", a, "Operation4");
                context.Logger.LogLine("Operation4");

                var e = await  orchestration.CallTaskAsync<OperationResult>("Quotient", a, "Operation5");
                context.Logger.LogLine("Operation5");
                /// Start timer
                await orchestration.StartTimerAsync("30SecTimer", new TimeSpan(0, 0, 0, 30, 0));
                context.Logger.LogLine("30SecTimer");

                /// Wait for user input
                var approved = await orchestration.WaitForEventAsync<bool>("Approve");
                context.Logger.LogLine("Approved");

                /// Complete workflow
                await orchestration.CompleteWorkflowAsync(e);
                context.Logger.LogLine("Complete");
            }
            catch (Exception ex)
            {
                /// Fail workflow
                await orchestration.FailWorkflowAsync(ex);
                context.Logger.LogLine("Fail");
                context.Logger.LogLine(ex.Message);
                context.Logger.LogLine(ex.StackTrace);
            }

            var currentState = await orchestration.GetCurrentState();
            return currentState;
        }

        /// <summary>
        /// Lambda function
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Model.Workflow Process(Request request, ILambdaContext context)
        {
            var result = ProcessAsync(request, context);
            result.Wait();
            return result.Result;
        }
    }
}
