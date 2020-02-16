using Newtonsoft.Json;
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
        public DateTime ScheduledDateTime { get; set; }
        public DateTime StartedDateTime { get; set; }
        public DateTime SucceededDateTime { get; set; }
        public DateTime FailedDateTime { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(new {
                OrchestrationId,
                Status,
                ScheduledDateTime,
                StartedDateTime,
                SucceededDateTime,
                FailedDateTime });
        }
    }
}
