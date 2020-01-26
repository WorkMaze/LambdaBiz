using LambdaBiz.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace LambdaBiz
{
    public abstract class OrchestrationContext
    {
        protected abstract IEnumerable<Activity> GetCurrentContext();
        protected Status GetStatus(ActivityType activityType, string name)
        {
            var activities = GetCurrentContext();

            Status status = Status.FAILED;

            foreach(var activity in activities)
            {
                if(activity.Name == name && activityType == activity.ActivityType)
                {
                    status = activity.Status;
                    break;
                }
            }

            return status;
        }
    }
}
