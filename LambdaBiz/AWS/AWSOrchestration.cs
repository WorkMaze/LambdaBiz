using Amazon;
using Amazon.DynamoDBv2;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using LambdaBiz.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LambdaBiz.REST;

namespace LambdaBiz.AWS
{
	public class AWSOrchestration : OrchestrationContext, IOrchestration
	{
		private AmazonSimpleWorkflowClient _amazonSimpleWorkflowClient;
        private string _lambdaRole;
        private string _orchestrationId;
        private IPersistantStore _store;
        internal AWSOrchestration(AmazonSimpleWorkflowClient amazonSimpleWorkflowClient,string orchestrationId,IPersistantStore store,string lambdaRole)
		{
			_amazonSimpleWorkflowClient = amazonSimpleWorkflowClient;
			_orchestrationId = orchestrationId;
            _store = store;
            _lambdaRole = lambdaRole;
        }

		#region Task
		public async Task<T> CallTaskAsync<T>(string functionName, object input, string id)
		{			
			var result = await CallLambdaAsync(functionName, input, id);

			return JsonConvert.DeserializeObject<T>(result);
		}

		public async Task<object> CallTaskAsync(string functionName, object input, string id)
		{
			var result = await CallLambdaAsync(functionName, input, id);

			return JsonConvert.DeserializeObject(result);
		}
		private async Task<string> CallLambdaAsync(string functionName, object input, string id)
		{
			var result = string.Empty;

            Activity activity = null;

            var workflowContext = await GetCurrentContext();

			if (workflowContext != null && workflowContext.Status == Status.STARTED)
			{
				var status = GetStatus(Model.ActivityType.Task, functionName, id, workflowContext);

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
                }
                else
                {
                    await SetMarker(Model.ActivityType.Task, functionName, id, status, workflowContext.ReferenceToken);
                    await RaiseEventAsync(Constants.LAMBDA_BIZ_EVENT, _orchestrationId, Constants.LAMBDA_BIZ_EVENT);
                    activity = FindActivity(Model.ActivityType.Task, id, functionName, workflowContext.Activities);
                   
                }

                if (status == Status.NONE || status == Status.SCHEDULED || status == Status.STARTED)
                { 
					var waitStatus = Status.NONE;
                    Workflow workflowWaitContext = null;
					do
					{
						workflowWaitContext = await GetCurrentContext();
						waitStatus = GetStatus(Model.ActivityType.Task, functionName, id, workflowWaitContext);
                        await SetMarker(Model.ActivityType.Task, functionName, id, waitStatus, workflowWaitContext.ReferenceToken);
                        await RaiseEventAsync(Constants.LAMBDA_BIZ_EVENT, _orchestrationId, Constants.LAMBDA_BIZ_EVENT);
					}
					while (waitStatus != Status.SUCCEEDED && waitStatus != Status.FAILED && waitStatus != Status.TIMEOUT);

					activity = FindActivity(Model.ActivityType.Task, id, functionName, workflowWaitContext.Activities);

				}
			}

            if (activity.Status == Status.FAILED)
                throw new Exception(activity.FailureDetails);

            if (activity.Status == Status.TIMEOUT)
                throw new Exception("Time-Out");

            result = activity.Result;
            return result;
		}

		#endregion

		#region Event

		public async Task RaiseEventAsync(string eventName, string orchestrationId, object eventArgs)
		{
			await _amazonSimpleWorkflowClient.SignalWorkflowExecutionAsync(new SignalWorkflowExecutionRequest
			{
				Input = JsonConvert.SerializeObject(eventArgs),
				WorkflowId = _orchestrationId,
				Domain = Constants.LAMBDA_BIZ_DOMAIN,
				SignalName = eventName
			});
			
		}

		public async  Task<T> WaitForEventAsync<T>(string eventName)
		{
			var result = await WaitForSignal(eventName);

			return JsonConvert.DeserializeObject<T>(result);
		}

		public async Task<object> WaitForEventAsync(string eventName)
		{
			var result = await WaitForSignal(eventName);

			return JsonConvert.DeserializeObject(result);
		}

		private async Task<string> WaitForSignal(string signalName)
		{
			var result = string.Empty;

			Status waitStatus = Status.STARTED;
			Workflow workflowWaitContext = null;
			
			do
			{
				workflowWaitContext = await GetCurrentContext();

                if (workflowWaitContext != null)
                {
                    waitStatus = GetStatus(Model.ActivityType.Event, signalName, signalName, workflowWaitContext);
                    await SetMarker(Model.ActivityType.Event, signalName, signalName, waitStatus, workflowWaitContext.ReferenceToken);
                    await RaiseEventAsync(Constants.LAMBDA_BIZ_EVENT, _orchestrationId, Constants.LAMBDA_BIZ_EVENT);
                }
            }
			while (waitStatus != Status.SUCCEEDED && workflowWaitContext != null && workflowWaitContext.Status == Status.STARTED);

			var activity = FindActivity(Model.ActivityType.Event, signalName, signalName, workflowWaitContext.Activities);

			result = activity.Result;		
			

			return result;
		}

		#endregion

		#region Timer
		public async Task StartTimerAsync(string timerName, TimeSpan timeSpan)
		{
			
			var workflowContext = await GetCurrentContext();

			if (workflowContext != null && workflowContext.Status == Status.STARTED)
			{
				var status = GetStatus(Model.ActivityType.Task, timerName, timerName, workflowContext);

                if (status == Status.NONE)
                {
                    var decisionRequest = new RespondDecisionTaskCompletedRequest
                    {
                        TaskToken = workflowContext.ReferenceToken,
                        Decisions = new List<Decision>
                        {
                            new Decision
                            {
                                DecisionType = DecisionType.StartTimer,
                                StartTimerDecisionAttributes = new StartTimerDecisionAttributes
                                {
                                    TimerId = timerName,
                                    StartToFireTimeout = timeSpan.TotalSeconds.ToString()
                                }
                            }
                        }
                    };

                    await _amazonSimpleWorkflowClient.RespondDecisionTaskCompletedAsync(decisionRequest);
                }
                else
                {
                    await SetMarker(Model.ActivityType.Task, timerName, timerName, status, workflowContext.ReferenceToken);
                    await RaiseEventAsync(Constants.LAMBDA_BIZ_EVENT, _orchestrationId, Constants.LAMBDA_BIZ_EVENT);
                }

                if (status == Status.NONE || status == Status.SCHEDULED || status == Status.STARTED)
                {
                    var waitStatus = Status.NONE;
				    Workflow workflowWaitContext = null;

				    do
				    {
					    workflowWaitContext = await GetCurrentContext();
					    waitStatus = GetStatus(Model.ActivityType.Timer, timerName, timerName, workflowWaitContext);
                        await SetMarker(Model.ActivityType.Timer, timerName, timerName, waitStatus, workflowWaitContext.ReferenceToken);
                        await RaiseEventAsync(Constants.LAMBDA_BIZ_EVENT, _orchestrationId, Constants.LAMBDA_BIZ_EVENT);
                    }
				    while (waitStatus != Status.SUCCEEDED);

				}
			}

			
		}
		#endregion

		#region Workflow

		public async Task StartWorkflowAsync(object input)
		{
            
            try
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
                    },
                    ExecutionStartToCloseTimeout = "31536000",
                    TaskStartToCloseTimeout = "NONE",
                    ChildPolicy = ChildPolicy.TERMINATE,
                    LambdaRole =_lambdaRole 
                }) ;
                
            }
            catch(WorkflowExecutionAlreadyStartedException)
            {
                
            }
            
           
			
		}

		public async Task CompleteWorkflowAsync(object result)
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
							DecisionType = DecisionType.CompleteWorkflowExecution,
							CompleteWorkflowExecutionDecisionAttributes = new CompleteWorkflowExecutionDecisionAttributes
							{
								Result = JsonConvert.SerializeObject(result)
							}
						}
					}
				};

				await _amazonSimpleWorkflowClient.RespondDecisionTaskCompletedAsync(decisionRequest);
                await _store.SetStatus(_orchestrationId, Status.SUCCEEDED);
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
                await _store.SetStatus(_orchestrationId, Status.FAILED);
            }
		}

		#endregion

		#region Polling

        private async Task SetMarker(Model.ActivityType activityType,string name, string id,Status status,string referenceToken)
        {
            var decisionRequest = new RespondDecisionTaskCompletedRequest
            {
                TaskToken = referenceToken,
                Decisions = new List<Decision>
                        {
                            new Decision
                            {
                                DecisionType = DecisionType.RecordMarker,
                                RecordMarkerDecisionAttributes = new RecordMarkerDecisionAttributes
                                {
                                    MarkerName = Constants.LAMBDA_BIZ_RECORD_MARKER_NAME + "_" + name +"_" + id,
                                    Details = activityType.ToString()+ "_" + status.ToString()
                                }
                            }
                        }
            };

            await _amazonSimpleWorkflowClient.RespondDecisionTaskCompletedAsync(decisionRequest);
        }
		protected override async Task<Workflow>  GetCurrentContext()
		{
			Workflow workflow = null;
			IList<Activity> activityList = new List<Activity>();
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

                    if(_store != null)
                    {
                        if (!await _store.StoreExistsAsync())
                            await _store.CreateStoreAsync();
                    }
				}

				foreach (HistoryEvent historyEvent in historyEvents)
				{
					if (historyEvent.EventType == EventType.WorkflowExecutionSignaled)
					{
						
                        var activity = FindActivity(Model.ActivityType.Event, historyEvent.WorkflowExecutionSignaledEventAttributes.SignalName, activityList);

                        if (activity == null)
                        {
                            activity = new Activity
                            {
                                Result = historyEvent.WorkflowExecutionSignaledEventAttributes.Input,
                                Status = Status.SUCCEEDED,
                                ActivityType = Model.ActivityType.Event,
                                Name = historyEvent.WorkflowExecutionSignaledEventAttributes.SignalName,
                                ScheduledId = historyEvent.WorkflowExecutionSignaledEventAttributes.SignalName,
                                UniqueId = historyEvent.WorkflowExecutionSignaledEventAttributes.SignalName,
								SucceededDateTime = historyEvent.EventTimestamp
                            };

                            activityList.Add(activity);
                        }

						
					}
					if(historyEvent.EventType == EventType.WorkflowExecutionStarted)
					{
                        workflow.Status = Status.STARTED;
                        workflow.OrchestrationId =
                            historyEvent.WorkflowExecutionStartedEventAttributes.TaskList.Name.Replace(Constants.LAMBDA_BIZ_TASK_LIST, string.Empty);
                        workflow.StartedDateTime = historyEvent.EventTimestamp;
					}
                    if(historyEvent.EventType == EventType.WorkflowExecutionFailed)
                    {                       
                        workflow.Status = Status.FAILED;
                        workflow.FailedDateTime = historyEvent.EventTimestamp;
                    }
                    if (historyEvent.EventType == EventType.WorkflowExecutionCompleted)
                    {                        
                        workflow.Status = Status.SUCCEEDED;
                        workflow.SucceededDateTime = historyEvent.EventTimestamp;
                    }
                    if (historyEvent.EventType == EventType.LambdaFunctionScheduled)
					{
						
                        var activity = FindActivity(Model.ActivityType.Task, historyEvent.LambdaFunctionScheduledEventAttributes.Id.ToString(), activityList);

                        if (activity == null)
                        {
                            activity = new Activity
                            {
                                ActivityType = Model.ActivityType.Task,
                                Name = historyEvent.LambdaFunctionScheduledEventAttributes.Name,
                                ScheduledId = historyEvent.EventId.ToString(),
                                UniqueId = historyEvent.LambdaFunctionScheduledEventAttributes.Control,
                                Status = Status.SCHEDULED,
                                ScheduledDateTime = historyEvent.EventTimestamp

							};

                            activityList.Add(activity);
                        }

					}
                    if(historyEvent.EventType == EventType.StartLambdaFunctionFailed)
                    {

                        var activity = FindActivity(Model.ActivityType.Task, historyEvent.StartLambdaFunctionFailedEventAttributes.ScheduledEventId.ToString(), activityList);
                        activity.Result = historyEvent.StartLambdaFunctionFailedEventAttributes.Message;
                        activity.FailureDetails = historyEvent.StartLambdaFunctionFailedEventAttributes.Cause;
                        activity.Status = Status.FAILED;
                        activity.FailedDateTime = historyEvent.EventTimestamp;
                    }
                    if(historyEvent.EventType == EventType.LambdaFunctionStarted)
                    {
                        var activity = FindActivity(Model.ActivityType.Task, historyEvent.LambdaFunctionStartedEventAttributes.ScheduledEventId.ToString(), activityList);
						                       
                        activity.Status = Status.STARTED;
                        activity.StartedDateTime = historyEvent.EventTimestamp;
                       
                    }
                    if (historyEvent.EventType == EventType.LambdaFunctionCompleted)
					{
						var activity = FindActivity(Model.ActivityType.Task, historyEvent.LambdaFunctionCompletedEventAttributes.ScheduledEventId.ToString(), activityList);
						activity.Result = historyEvent.LambdaFunctionCompletedEventAttributes.Result;
						activity.Status = Status.SUCCEEDED;
						activity.SucceededDateTime = historyEvent.EventTimestamp;
					}
					if (historyEvent.EventType == EventType.LambdaFunctionFailed)
					{
						var activity = FindActivity(Model.ActivityType.Task, historyEvent.LambdaFunctionFailedEventAttributes.ScheduledEventId.ToString(), activityList);
						activity.Result = historyEvent.LambdaFunctionFailedEventAttributes.Details;
                        activity.FailureDetails = historyEvent.LambdaFunctionFailedEventAttributes.Reason;
                        activity.Status = Status.FAILED;
                        activity.FailedDateTime = historyEvent.EventTimestamp;
                    }
					if (historyEvent.EventType == EventType.LambdaFunctionTimedOut)
					{
						var activity = FindActivity(Model.ActivityType.Task, historyEvent.LambdaFunctionTimedOutEventAttributes.ScheduledEventId.ToString(), activityList);
                        activity.Status = Status.TIMEOUT;
                        activity.TmedOutDateTime = historyEvent.EventTimestamp;
					}

					if (historyEvent.EventType == EventType.TimerStarted)
					{
						
						var activity = new Activity
						{
							ActivityType = Model.ActivityType.Timer,
							Name = historyEvent.TimerStartedEventAttributes.TimerId,
							ScheduledId = historyEvent.EventId.ToString(),
							UniqueId = historyEvent.TimerStartedEventAttributes.TimerId,
							Status = Status.STARTED,
                            StartedDateTime = historyEvent.EventTimestamp
						};

						activityList.Add(activity);
						
					}
					if (historyEvent.EventType == EventType.TimerFired)
					{
						var activity = FindActivity(Model.ActivityType.Timer, historyEvent.TimerFiredEventAttributes.StartedEventId.ToString(), activityList);
                        activity.Status = Status.SUCCEEDED;
                        activity.SucceededDateTime = historyEvent.EventTimestamp;
                    }
                    if (historyEvent.EventType == EventType.ActivityTaskScheduled)
                    {
                       
                        var activity = FindActivity(Model.ActivityType.Task, historyEvent.ActivityTaskScheduledEventAttributes.ActivityId, activityList);
                        if(activity == null)
                        { 
                            activity = new Activity
                            {
                                ActivityType = Model.ActivityType.Task,
                                Name = historyEvent.ActivityTaskScheduledEventAttributes.ActivityType.Name,
                                ScheduledId = historyEvent.EventId.ToString(),
                                UniqueId = historyEvent.ActivityTaskScheduledEventAttributes.Control,
                                Status = Status.SCHEDULED,
                                ScheduledDateTime = historyEvent.EventTimestamp

							};

                            activityList.Add(activity);
                        }
                    }
                    if (historyEvent.EventType == EventType.ActivityTaskStarted)
                    {
                        var activity = FindActivity(Model.ActivityType.Task, historyEvent.ActivityTaskStartedEventAttributes.ScheduledEventId.ToString(), activityList);
                        activity.Status = Status.STARTED;
                        activity.StartedDateTime = historyEvent.EventTimestamp;
                    }
                   
                    if (historyEvent.EventType == EventType.ActivityTaskFailed)
                    {
                        var activity = FindActivity(Model.ActivityType.Task, historyEvent.ActivityTaskFailedEventAttributes.ScheduledEventId.ToString(), activityList);
                        activity.Result = historyEvent.ActivityTaskFailedEventAttributes.Details;
                        activity.Status = Status.FAILED;
                        activity.FailedDateTime = historyEvent.EventTimestamp;
					}
                    if (historyEvent.EventType == EventType.ActivityTaskCompleted)
                    {
                        var activity = FindActivity(Model.ActivityType.Task, historyEvent.ActivityTaskCompletedEventAttributes.ScheduledEventId.ToString(), activityList);
                        activity.Result = historyEvent.ActivityTaskCompletedEventAttributes.Result;
                        activity.Status = Status.SUCCEEDED;
                        activity.SucceededDateTime = historyEvent.EventTimestamp;
					}
                    if (historyEvent.EventType == EventType.ActivityTaskTimedOut)
                    {
                        var activity = FindActivity(Model.ActivityType.Task, historyEvent.ActivityTaskTimedOutEventAttributes.ScheduledEventId.ToString(), activityList);

						activity.Status = Status.TIMEOUT;
                        activity.TmedOutDateTime = historyEvent.EventTimestamp;
                    }

                }
                workflow.Activities = activityList;
                Task loggingTask =_store.LogStateAsync(workflow);
            }
			
			return workflow;
		}

		#endregion

		#region Status
		public async Task<Workflow> GetCurrentState()
		{
			return await _store.GetCurrentStateAsync(this._orchestrationId);
		}
		#endregion

		#region REST
		public async Task<T> CallGetAsync<T>(string url, string queryString, Dictionary<string, string> headers, string id)
		{
            var response = await CallServiceAsync(url, "get", queryString, null, headers, id);
			return JsonConvert.DeserializeObject<T>(response);
		}

		public async Task<object> CallGetAsync(string url, string queryString, Dictionary<string, string> headers, string id)
		{
			var response = await CallServiceAsync(url, "get", queryString, null, headers, id);
            return JsonConvert.DeserializeObject(response);
		}

		public async Task<T> CallDeleteAsync<T>(string url, string queryString, Dictionary<string, string> headers, string id)
		{
			var response = await CallServiceAsync(url, "delete", queryString, null, headers,id);
			return JsonConvert.DeserializeObject<T>(response);
		}

		public async Task<object> CallDeleteAsync(string url, string queryString, Dictionary<string, string> headers, string id)
		{
			var response = await CallServiceAsync(url, "delete", queryString, null, headers,id);
			return JsonConvert.DeserializeObject(response);
		}

		public async Task<T> CallPostAsync<T>(string url, string queryString, object body, Dictionary<string, string> headers, string id)
		{
			var response = await CallServiceAsync(url, "post", queryString, body, headers,id);
			return JsonConvert.DeserializeObject<T>(response);
		}

		public async Task<object> CallPostAsync(string url, string queryString, object body, Dictionary<string, string> headers, string id)
		{
			var response = await CallServiceAsync(url, "post", queryString, body, headers,id);
			return JsonConvert.DeserializeObject(response);
		}

		public async Task<T> CallPutAsync<T>(string url, string queryString, object body, Dictionary<string, string> headers, string id)
		{
			var response = await CallServiceAsync(url, "put", queryString, body, headers,id);
			return JsonConvert.DeserializeObject<T>(response);
		}

		public async Task<object> CallPutAsync(string url, string queryString, object body, Dictionary<string, string> headers, string id)
		{
			var response = await CallServiceAsync(url, "put", queryString, null, headers,id);
			return JsonConvert.DeserializeObject(response);
		}

        private async Task<string> CallServiceAsync(string url, string method, string queryString, object input, Dictionary<string, string> headers, string id)
        {
            var result = string.Empty;
            Activity activity = null;

            var restConfig = new RESTConfig
            {
                Body = JsonConvert.SerializeObject(input),
                Headers = headers,
                Method = method,
                QueryString = queryString,
                Url = url
            };

            var workflowContext = await GetCurrentContext();

            if (workflowContext != null && workflowContext.Status == Status.STARTED)
            {
                var status = GetStatus(Model.ActivityType.Task, Constants.LAMBDA_BIZ_ACTIVITY_TYPE, id, workflowContext);

                if (status == Status.NONE)
                {
                    var decisionRequest = new RespondDecisionTaskCompletedRequest
                    {
                        TaskToken = workflowContext.ReferenceToken,
                        Decisions = new List<Decision>
                        {
                            new Decision
                            {
                                DecisionType = DecisionType.ScheduleActivityTask,
                                ScheduleActivityTaskDecisionAttributes = new ScheduleActivityTaskDecisionAttributes
                                {
                                    Input = JsonConvert.SerializeObject(restConfig),
                                    ActivityId = id,
                                    Control = id,
                                    ActivityType = new Amazon.SimpleWorkflow.Model.ActivityType
                                    {
                                        Name = Constants.LAMBDA_BIZ_ACTIVITY_TYPE,
                                        Version = Constants.LAMBDA_BIZ_TYPE_VERSION
                                    },
                                    TaskList = new TaskList
                                    {
                                        Name = Constants.LAMBDA_BIZ_TASK_LIST + _orchestrationId
                                    },
                                    StartToCloseTimeout = "NONE",
                                    ScheduleToCloseTimeout = "NONE",
                                    ScheduleToStartTimeout = "NONE",
                                    HeartbeatTimeout = "NONE"
                                },
                            }
                        }
                    };

                    await _amazonSimpleWorkflowClient.RespondDecisionTaskCompletedAsync(decisionRequest);
                }
                else
                {
                    await SetMarker(Model.ActivityType.Task, Constants.LAMBDA_BIZ_ACTIVITY_TYPE, id, status, workflowContext.ReferenceToken);
                    await RaiseEventAsync(Constants.LAMBDA_BIZ_EVENT, _orchestrationId, Constants.LAMBDA_BIZ_EVENT);
                    activity = FindActivity(Model.ActivityType.Task, id, Constants.LAMBDA_BIZ_ACTIVITY_TYPE, workflowContext.Activities);
                    
                }

                if (status == Status.NONE || status == Status.SCHEDULED || status == Status.STARTED)
                {
                    var waitStatus = Status.NONE;
                    Workflow workflowWaitContext = null;


                    do
                    {
                        workflowWaitContext = await GetCurrentContext();
                        waitStatus = GetStatus(Model.ActivityType.Task, Constants.LAMBDA_BIZ_ACTIVITY_TYPE, id, workflowWaitContext);
                        await SetMarker(Model.ActivityType.Task, Constants.LAMBDA_BIZ_ACTIVITY_TYPE, id, waitStatus, workflowWaitContext.ReferenceToken);
                        await RaiseEventAsync(Constants.LAMBDA_BIZ_EVENT, _orchestrationId, Constants.LAMBDA_BIZ_EVENT);
                    }
                    while (waitStatus != Status.SUCCEEDED && waitStatus != Status.FAILED && waitStatus != Status.TIMEOUT);

                    activity = FindActivity(Model.ActivityType.Task, id, Constants.LAMBDA_BIZ_ACTIVITY_TYPE, workflowWaitContext.Activities);

                }
            }

            if (activity.Status == Status.FAILED)
                throw new Exception(activity.FailureDetails);

            if (activity.Status == Status.TIMEOUT)
                throw new Exception("Time-Out");

            result = activity.Result;
            return result;
        }

        #endregion
    }
}
