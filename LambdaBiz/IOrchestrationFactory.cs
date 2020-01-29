using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
	public interface IOrchestrationFactory
	{
		IOrchestration CreateOrchestration(string orchestrationId);
		IOrchestration CreateOrchestration(string orchestrationId, IServerlessContext context);
	}
}
