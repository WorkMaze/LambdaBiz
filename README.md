# LambdaBiz

**Long running stateful orchestrations in C# using AWS Lambda.**
AWS lambda enables users to write serverless functions. However, a lambda function can have a maximum execution time of 15 minutes after which it times out. Hence, it is not possible to write long running processes in AWS lambda. AWS has introduced step functions to overcome this shortcomming. However, there is a steep learning curve to learn the state machine language and the service itself comes at a premium cost.

The purpose of this project is to enable existing C# users of AWS to write long running orchestrations which are durable. 

## Durability
The framework relies upon the AWS SWF (Simple Workflow Framework) to maintain the durability of the orchestration. If a sequence of tasks has been executed and the sequence times out and is called again, the framework will not call the already executed tasks in the sequence and the orchestration will continue from the point where it was left during the last run.

## Orcestrating lambda tasks

            /// Initialize orchestration factory for AWS
            var orchestrationFactory = new AWSOrchestrationFactory(aws.AccessKeyID, aws.SecretAccessKey, aws.Region, true,aws.LambdaRole);

            /// Create a new orchestration
            var orchestration = await orchestrationFactory.CreateOrchestrationAsync("Sequence3");

            try {

                /// Start workflow
                await orchestration.StartWorkflowAsync("Workflow Started");

                /// Call AWS lambda task
                var a = await orchestration.CallTaskAsync<Numbers>("Number", new Numbers { Number1 = 15, Number2 = 5 }, "Operation1");

                var b = await orchestration.CallTaskAsync<OperationResult>("Sum", a, "Operation2");

                var c = await orchestration.CallTaskAsync<OperationResult>("Difference", a, "Operation3");

                var d = await orchestration.CallTaskAsync<OperationResult>("Product", a, "Operation4");

                var e = await orchestration.CallTaskAsync<OperationResult>("Quotient", a, "Operation5");

                /// Complete workflow
                await orchestration.CompleteWorkflowAsync(e);
            }
            catch(Exception ex)
            {
                /// Fail workflow
                await orchestration.FailWorkflowAsync(ex);
            }
