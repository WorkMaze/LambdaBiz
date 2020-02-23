using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    /// <summary>
    /// Interface to implement your own orchestration factory
    /// </summary>
	public interface IOrchestrationFactory
	{
		Task<IOrchestration>  CreateOrchestrationAsync(string orchestrationId);
	}
}
