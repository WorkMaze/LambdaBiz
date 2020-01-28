using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    public interface IOrchestration
    {
        void CreateOrchestration(string orchestrationId);
        void CreateOrchestration(string orchestrationId,IServerlessContext context);
        T CallTask<T>(string functionName, object input);
        Task<T> CallTaskAsync<T>(string functionName,object input);
        object CallTask(string functionName, object input);
        Task<object> CallTaskAsync(string functionName, object input);
        T WaitForEvent<T>(string eventName);
        Task<T> WaitForEventAsync<T>(string eventName);
        object WaitForEvent(string eventName);
        Task<object> WaitForEventAsync(string eventName);
        Task RaiseEventAsync(string eventName,string orchestrationId,object eventArgs);
        Task StartTimerAsync(string timerName, TimeSpan timeSpan);
    }
}
