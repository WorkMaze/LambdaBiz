using LambdaBiz.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    /// <summary>
    /// Interface to implmenet your own orchestration container
    /// </summary>
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

        #region REST
        Task<T> CallGetAsync<T>(string url, string queryString, Dictionary<string, string> headers, string id);
        Task<object> CallGetAsync(string url, string queryString, Dictionary<string, string> headers, string id);
        Task<T> CallDeleteAsync<T>(string url, string queryString, Dictionary<string, string> headers, string id);
        Task<object> CallDeleteAsync(string url, string queryString, Dictionary<string, string> headers, string id);
        Task<T> CallPostAsync<T>(string url, string queryString, object body, Dictionary<string, string> headers, string id);
        Task<object> CallPostAsync(string url, string queryString, object body, Dictionary<string, string> headers, string id);
        Task<T> CallPutAsync<T>(string url, string queryString, object body, Dictionary<string, string> headers, string id);
        Task<object> CallPutAsync(string url, string queryString, object body, Dictionary<string, string> headers, string id);
        #endregion
    }
}
