using LambdaBiz.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LambdaBiz
{
    public abstract class OrchestrationContext
    {
        protected abstract Task<IEnumerable<Activity>> GetCurrentContext();
        protected async Task<Status> GetStatus(ActivityType activityType, string name)
        {
            var activityList = await GetCurrentContext();

            Status status = Status.FAILED;

            foreach(var activity in activityList)
            {
                if(activity.Name == name && activityType == activity.ActivityType)
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
    }
}
