using System;
using System.Collections.Generic;
using System.Text;

namespace LambdaBiz.Model
{
	public class Workflow
	{
		public IEnumerable<Activity> Activities { get; set; }
		public Status Status { get; set; }
		public string OrchestrationId { get; set; }
		public string ReferenceToken { get; set; }
	}
}
