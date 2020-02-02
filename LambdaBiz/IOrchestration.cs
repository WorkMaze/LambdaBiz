using LambdaBiz.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    public interface IOrchestration
    {
        Task<T> CallTaskAsync<T>(string functionName,object input,string id);
        Task<object> CallTaskAsync(string functionName, object input, string id);
        Task<T> WaitForEventAsync<T>(string eventName);
        Task<object> WaitForEventAsync(string eventName);
        Task RaiseEventAsync(string eventName,string orchestrationId,object eventArgs);
        Task StartTimerAsync(string timerName, TimeSpan timeSpan);
		Task StartWorkflowAsync(object input);
		Task CompleteWorkflowAsync(object result);
		Task FailWorkflowAsync(object error);
        Task<Workflow> GetCurrentState();
    }
}
