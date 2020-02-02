using System;
using System.Collections.Generic;
using System.Text;

namespace LambdaBiz.Model
{
    public class Activity
    {
        public string Name { get; set; }
        public Status Status { get; set; }
        public ActivityType ActivityType { get; set; }
		public string UniqueId { get; set; }
		public string ScheduledId { get; set; }
		public string FailureDetails { get; set; }
		public string Result { get; set; }
        public DateTime StartedDateTime { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public DateTime SucceededDateTime { get; set; }
        public DateTime FailedDateTime { get; set; }
        public DateTime TmedOutDateTime { get; set; }
    }
}
