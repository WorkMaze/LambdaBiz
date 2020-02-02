using LambdaBiz.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    public abstract class OrchestrationContext
    {
        protected abstract Task<Workflow> GetCurrentContext();
        protected Status GetStatus(ActivityType activityType, string name,string uniqueId, Workflow workflow)
        {
            Status status = Status.NONE;

            foreach(var activity in workflow.Activities)
            {
                if(activity.Name == name && activityType == activity.ActivityType && activity.UniqueId == uniqueId)
                {
                    status = activity.Status;
                    break;
                }
            }

            return status;
        }

		protected Activity FindActivity(ActivityType activityType, string scheduleId, IEnumerable<Activity> activityList)
		{
			Activity retActivity = null;

			foreach (var activity in activityList)
			{
				if (activity.ScheduledId == scheduleId && activityType == activity.ActivityType)
				{
					retActivity = activity;
					break;
				}
			}

			return retActivity;
		}

		public Activity FindActivity(ActivityType activityType, string uniqueId,string name,IEnumerable<Activity> activityList)
		{
			Activity retActivity = null;

			foreach (var activity in activityList)
			{
				if (activity.UniqueId == uniqueId && activityType == activity.ActivityType && activity.Name == name)
				{
					retActivity = activity;
					break;
				}
			}

			return retActivity;
		}
	}
}
