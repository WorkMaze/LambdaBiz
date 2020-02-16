using LambdaBiz.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    public interface IPersistantStore
    {
        Task<bool> StoreExistsAsync();
        Task CreateStoreAsync();
        Task LogStateAsync(Workflow workflow);
        Task<Workflow> GetCurrentStateAsync(string orchestrationId);
        Task SetStatus(string orchestrationId, Status status);
        
    }
}
