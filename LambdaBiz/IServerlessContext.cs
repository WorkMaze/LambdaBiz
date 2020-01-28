using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    public interface IServerlessContext
    {
        T CallTask<T>(string functionName, object input);
        Task<T> CallTaskAsync<T>(string functionName, object input);
        object CallTask(string functionName, object input);
        Task<object> CallTaskAsync(string functionName, object input);
    }
}
