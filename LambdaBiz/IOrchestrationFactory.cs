using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
	public interface IOrchestrationFactory
	{
		Task<IOrchestration>  CreateOrchestrationAsync(string orchestrationId);
	}
}
