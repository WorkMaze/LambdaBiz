using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    public interface IServerlessContext
    {
        T CallTask<T>(string functionName);
        Task<T> CallTaskAsync<T>(string functionName);
        object CallTask(string functionName);
        Task<object> CallTaskAsync(string functionName);
    }
}
