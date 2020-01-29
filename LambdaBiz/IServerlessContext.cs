using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    public interface IServerlessContext
    {
        Task<T> CallTaskAsync<T>(string functionName, object input);
        Task<object> CallTaskAsync(string functionName, object input);
    }
}
