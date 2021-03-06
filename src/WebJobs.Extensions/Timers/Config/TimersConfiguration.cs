﻿using System;
using Microsoft.Azure.WebJobs.Extensions.Timers.Scheduling;

namespace Microsoft.Azure.WebJobs.Extensions.Timers.Config
{
    /// <summary>
    /// Configuration object for <see cref="TimerTriggerAttribute"/> decorated job functions.
    /// </summary>
    public class TimersConfiguration
    {
        private ScheduleMonitor _scheduleMonitor;

        /// <summary>
        /// Constructs a new instance
        /// </summary>
        public TimersConfiguration()
        {
            ScheduleMonitor = new FileSystemScheduleMonitor();
        }

        /// <summary>
        /// Gets or sets the schedule monitor used to persist
        /// schedule occurrences and monitor execution.
        /// </summary>
        public ScheduleMonitor ScheduleMonitor
        {
            get
            {
                return _scheduleMonitor;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _scheduleMonitor = value;
            }
        }
    }
}
