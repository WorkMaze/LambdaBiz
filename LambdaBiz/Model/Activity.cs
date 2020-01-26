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
    }
}
