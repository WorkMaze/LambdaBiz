using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using LambdaBiz.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz.AWS
{
	public class AWSOrchestration : OrchestrationContext, IOrchestration
	{
		private AmazonSimpleWorkflowClient _amazonSimpleWorkflowClient;
		public AWSOrchestration(string awsAccessKey, string awsSecretAccessKey, string awsRegion)
		{
			_amazonSimpleWorkflowClient = new AmazonSimpleWorkflowClient(awsAccessKey, awsSecretAccessKey, RegionEndpoint.GetBySystemName(awsRegion));
		}
		public T CallTask<T>(string functionName, object input)
		{
			throw new NotImplementedException();
		}

		public object CallTask(string functionName, object input)
		{
			throw new NotImplementedException();
		}

		public Task<T> CallTaskAsync<T>(string functionName, object input)
		{
			throw new NotImplementedException();
		}

		public Task<object> CallTaskAsync(string functionName, object input)
		{
			throw new NotImplementedException();
		}

		public void CreateOrchestration(string orchestrationId)
		{
			throw new NotImplementedException();
		}

		public void CreateOrchestration(string orchestrationId, IServerlessContext context)
		{
			throw new NotImplementedException();
		}

		public Task RaiseEventAsync(string eventName, string orchestrationId, object eventArgs)
		{
			throw new NotImplementedException();
		}

		public Task StartTimerAsync(string timerName, TimeSpan timeSpan)
		{
			throw new NotImplementedException();
		}

		public T WaitForEvent<T>(string eventName)
		{
			throw new NotImplementedException();
		}

		public object WaitForEvent(string eventName)
		{
			throw new NotImplementedException();
		}

		public Task<T> WaitForEventAsync<T>(string eventName)
		{
			throw new NotImplementedException();
		}

		public Task<object> WaitForEventAsync(string eventName)
		{
			throw new NotImplementedException();
		}

		protected override async Task<IEnumerable<Activity>> GetCurrentContext()
		{
			IList<Activity> activityList = null;
		    PollForDecisionTaskResponse decisionTaskResponse = null;
			List<HistoryEvent> historyEvents = new List<HistoryEvent>();
			string taskToken = null;
			string nextPageToken = null;
			do
			{
				PollForDecisionTaskRequest decisionTaskRequest = new PollForDecisionTaskRequest
				{
					//Domain = _domain,
					Identity = Guid.NewGuid().ToString(),
					//TaskList = new TaskList { Name },
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
				foreach (HistoryEvent historyEvent in historyEvents)
				{
					if (historyEvent.EventType == EventType.LambdaFunctionScheduled)
					{
						if (activityList == null)
						{
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
					}
					if (historyEvent.EventType == EventType.LambdaFunctionCompleted)
					{
						var activity = FindActivity(Model.ActivityType.Task, historyEvent.LambdaFunctionCompletedEventAttributes.ScheduledEventId.ToString(), activityList);

						activity.Status = Status.SUCCEEDED;
					}
					if (historyEvent.EventType == EventType.LambdaFunctionFailed)
					{
						var activity = FindActivity(Model.ActivityType.Task, historyEvent.LambdaFunctionFailedEventAttributes.ScheduledEventId.ToString(), activityList);

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
						{
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

			return activityList;
		}
	}
}
