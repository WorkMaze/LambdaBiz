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
        Task<T> WaitForEventAsync<T>(string eventName, string id);
        Task<object> WaitForEventAsync(string eventName, string id);
        Task RaiseEventAsync(string eventName,string orchestrationId,object eventArgs);
        Task StartTimerAsync(string timerName, TimeSpan timeSpan);
		Task StartWorkflowAsync();
		Task CompleteWorkflowAsync();
		Task FailWorkflowAsync();
    }
}
