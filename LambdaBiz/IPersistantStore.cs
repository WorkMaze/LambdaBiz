using LambdaBiz.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    /// <summary>
    /// Interface to implement your own persistent store
    /// </summary>
    public interface IPersistantStore
    {
        Task<bool> StoreExistsAsync();
        Task CreateStoreAsync();
        Task LogStateAsync(Workflow workflow);
        Task<Workflow> GetCurrentStateAsync(string orchestrationId);
        Task SetStatus(string orchestrationId, Status status);
        
    }
}
