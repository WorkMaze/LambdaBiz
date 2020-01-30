using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using LambdaBiz.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz.AWS
{
	public class AWSOrchestration : OrchestrationContext, IOrchestration
	{
		private AmazonSimpleWorkflowClient _amazonSimpleWorkflowClient;
		private string _orchestrationId;
		internal AWSOrchestration(AmazonSimpleWorkflowClient amazonSimpleWorkflowClient,string orchestrationId)
		{
			_amazonSimpleWorkflowClient = amazonSimpleWorkflowClient;
			_orchestrationId = orchestrationId;

		}
		public async Task<T> CallTaskAsync<T>(string functionName, object input, string id)
		{			
			var result = await CallLambdaAsync(functionName, input, id);

			return JsonConvert.DeserializeObject<T>(result);
		}

		private async Task<string> CallLambdaAsync(string functionName, object input, string id)
		{
			var result = string.Empty;

			var workflowContext = await GetCurrentContext();

			if (workflowContext != null && workflowContext.Status == Status.STARTED)
			{
				var status = await GetStatus(Model.ActivityType.Task, functionName, id, workflowContext);

				if (status == Status.NONE)
				{
					var decisionRequest = new RespondDecisionTaskCompletedRequest
					{
						TaskToken = workflowContext.ReferenceToken,
						Decisions = new List<Decision>
						{
							new Decision
							{
								DecisionType = DecisionType.ScheduleLambdaFunction,
								ScheduleLambdaFunctionDecisionAttributes = new ScheduleLambdaFunctionDecisionAttributes
								{
									Input = JsonConvert.SerializeObject(input),
									Name = functionName,
									Id = id,
									Control = id
								}
							}
						}
					};

					await _amazonSimpleWorkflowClient.RespondDecisionTaskCompletedAsync(decisionRequest);

					var waitStatus = Status.NONE;
					Workflow workflowWaitContext = null;


					do
					{
						workflowWaitContext = await GetCurrentContext();
						waitStatus = await GetStatus(Model.ActivityType.Task, functionName, id, workflowWaitContext);
					}
					while (waitStatus != Status.SUCCEEDED && waitStatus != Status.FAILED && waitStatus != Status.TIMEOUT);

					var activity = FindActivity(Model.ActivityType.Task, id, functionName, workflowWaitContext.Activities);

					if (waitStatus == Status.FAILED)
						throw new Exception(activity.FailureDetails);

					if (waitStatus == Status.TIMEOUT)
						throw new Exception("Time-Out");

					result = activity.Result;
				}
			}

			return result;
		}

		public async Task<object> CallTaskAsync(string functionName, object input, string id)
		{
			return await CallLambdaAsync(functionName, input, id);
		}

		public async Task CompleteWorkflowAsync(object result)
		{
			var workflowContext = await GetCurrentContext();

			if(workflowContext != null)
			{
				var decisionRequest = new RespondDecisionTaskCompletedRequest
				{
					TaskToken = workflowContext.ReferenceToken,
					Decisions = new List<Decision>
					{
						new Decision
						{
							DecisionType = DecisionType.CompleteWorkflowExecution,
							CompleteWorkflowExecutionDecisionAttributes = new CompleteWorkflowExecutionDecisionAttributes
							{
								Result = JsonConvert.SerializeObject(result)
							}
						}
					}
				};

				await _amazonSimpleWorkflowClient.RespondDecisionTaskCompletedAsync(decisionRequest);
			}
		}

		public async Task FailWorkflowAsync(object error)
		{
			var workflowContext = await GetCurrentContext();

			if (workflowContext != null)
			{
				var decisionRequest = new RespondDecisionTaskCompletedRequest
				{
					TaskToken = workflowContext.ReferenceToken,
					Decisions = new List<Decision>
					{
						new Decision
						{
							DecisionType = DecisionType.FailWorkflowExecution,
							FailWorkflowExecutionDecisionAttributes = new FailWorkflowExecutionDecisionAttributes
							{
								Details = JsonConvert.SerializeObject(error)
							}
						}
					}
				};

				await _amazonSimpleWorkflowClient.RespondDecisionTaskCompletedAsync(decisionRequest);
			}
		}

		public Task RaiseEventAsync(string eventName, string orchestrationId, object eventArgs)
		{
			throw new NotImplementedException();
		}

		public Task StartTimerAsync(string timerName, TimeSpan timeSpan)
		{
			throw new NotImplementedException();
		}

		public async Task StartWorkflowAsync(object input)
		{
			var workflowContext = GetCurrentContext();

			if (workflowContext == null)
			{
				await _amazonSimpleWorkflowClient.StartWorkflowExecutionAsync(new StartWorkflowExecutionRequest
				{
					WorkflowId = _orchestrationId,
					Domain = Constants.LAMBDA_BIZ_DOMAIN,
					WorkflowType = new WorkflowType
					{
						Name = Constants.LAMBDA_BIZ_WORKFLOW_TYPE,
						Version = Constants.LAMBDA_BIZ_TYPE_VERSION
					},
					Input = JsonConvert.SerializeObject(input),
					TaskList = new TaskList
					{
						Name = Constants.LAMBDA_BIZ_TASK_LIST + _orchestrationId
					}
				});
			}
		}

		public Task<T> WaitForEventAsync<T>(string eventName, string id)
		{
			throw new NotImplementedException();
		}

		public Task<object> WaitForEventAsync(string eventName, string id)
		{
			throw new NotImplementedException();
		}

		protected override async Task<Workflow> GetCurrentContext()
		{
			Workflow workflow = null;
			IList<Activity> activityList = null;
		    PollForDecisionTaskResponse decisionTaskResponse = null;
			List<HistoryEvent> historyEvents = new List<HistoryEvent>();
			string taskToken = null;
			string nextPageToken = null;
			do
			{
				var decisionTaskRequest = new PollForDecisionTaskRequest
				{
					Domain = Constants.LAMBDA_BIZ_DOMAIN,
					Identity = Guid.NewGuid().ToString(),
					TaskList = new TaskList
					{
						Name = Constants.LAMBDA_BIZ_TASK_LIST + _orchestrationId
					},
					NextPageToken = nextPageToken
				};

				decisionTaskResponse = await _amazonSimpleWorkflowClient.PollForDecisionTaskAsync(decisionTaskRequest);

				taskToken = decisionTaskResponse.DecisionTask.TaskToken;

				if (!string.IsNullOrEmpty(taskToken))
				{
					var decisionTask = decisionTaskResponse.DecisionTask;

					foreach (HistoryEvent historyEvent in decisionTask.Events)
						historyEvents.Add(historyEvent);
				}
			}
			while (!string.IsNullOrEmpty(nextPageToken = decisionTaskResponse.DecisionTask.NextPageToken));

			if (historyEvents.Count > 0)
			{
				if (workflow == null)
				{
					workflow = new Workflow();
					workflow.ReferenceToken = taskToken;
				}

				foreach (HistoryEvent historyEvent in historyEvents)
				{
					if(historyEvent.EventType == EventType.WorkflowExecutionStarted)
					{
						

						workflow.Status = Status.STARTED;
						workflow.OrchestrationId = 
							historyEvent.WorkflowExecutionStartedEventAttributes.TaskList.Name.Replace(Constants.LAMBDA_BIZ_TASK_LIST, string.Empty);

					}
					if (historyEvent.EventType == EventType.LambdaFunctionScheduled)
					{
						if (activityList == null)
							activityList = new List<Activity>();
						

						var activity = new Activity
						{
							ActivityType = Model.ActivityType.Task,
							Name = historyEvent.LambdaFunctionScheduledEventAttributes.Name,
							ScheduledId = historyEvent.LambdaFunctionScheduledEventAttributes.Id,
							UniqueId = historyEvent.LambdaFunctionScheduledEventAttributes.Control,
							Status = Status.STARTED,

						};

						activityList.Add(activity);

					}
					if (historyEvent.EventType == EventType.LambdaFunctionCompleted)
					{
						var activity = FindActivity(Model.ActivityType.Task, historyEvent.LambdaFunctionCompletedEventAttributes.ScheduledEventId.ToString(), activityList);
						activity.Result = historyEvent.LambdaFunctionCompletedEventAttributes.Result;
						activity.Status = Status.SUCCEEDED;
					}
					if (historyEvent.EventType == EventType.LambdaFunctionFailed)
					{
						var activity = FindActivity(Model.ActivityType.Task, historyEvent.LambdaFunctionFailedEventAttributes.ScheduledEventId.ToString(), activityList);
						activity.Result = historyEvent.LambdaFunctionFailedEventAttributes.Details;
						activity.Status = Status.FAILED;
					}
					if (historyEvent.EventType == EventType.LambdaFunctionTimedOut)
					{
						var activity = FindActivity(Model.ActivityType.Task, historyEvent.LambdaFunctionTimedOutEventAttributes.ScheduledEventId.ToString(), activityList);

						activity.Status = Status.TIMEOUT;
					}

					if (historyEvent.EventType == EventType.TimerStarted)
					{
						if (activityList == null)
							activityList = new List<Activity>();

						var activity = new Activity
						{
							ActivityType = Model.ActivityType.Timer,
							Name = historyEvent.TimerStartedEventAttributes.TimerId,
							ScheduledId = historyEvent.TimerStartedEventAttributes.TimerId,
							UniqueId = historyEvent.TimerStartedEventAttributes.Control,
							Status = Status.STARTED
						};

						activityList.Add(activity);
						
					}
					if (historyEvent.EventType == EventType.TimerFired)
					{
						var activity = FindActivity(Model.ActivityType.Task, historyEvent.TimerFiredEventAttributes.TimerId, activityList);

						activity.Status = Status.SUCCEEDED;
					}
					if (historyEvent.EventType == EventType.ActivityTaskScheduled)
					{
						if (activityList == null)
						{
							activityList = new List<Activity>();
							var activity = new Activity
							{
								ActivityType = Model.ActivityType.Task,
								Name = historyEvent.ActivityTaskScheduledEventAttributes.ActivityType.Name,
								ScheduledId = historyEvent.ActivityTaskScheduledEventAttributes.ActivityId,
								UniqueId = historyEvent.ActivityTaskScheduledEventAttributes.Control,
								Status = Status.STARTED,
								
							};

							activityList.Add(activity);
						}						
					}
					if (historyEvent.EventType == EventType.ActivityTaskCompleted)
					{
						var activity = FindActivity(Model.ActivityType.Task, historyEvent.ActivityTaskCompletedEventAttributes.ScheduledEventId.ToString(), activityList);

						activity.Status = Status.SUCCEEDED;
					}
				}
			}
			workflow.Activities = activityList;
			return workflow;
		}
	}
}
