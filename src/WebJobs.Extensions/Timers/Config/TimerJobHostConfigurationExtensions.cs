﻿using System;
using Microsoft.Azure.WebJobs.Extensions.Timers.Config;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Config;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Extension class used to register timer extensions.
    /// </summary>
    public static class TimerJobHostConfigurationExtensions
    {
        /// <summary>
        /// Enables use of the Timer extensions
        /// </summary>
        /// <param name="config">The <see cref="JobHostConfiguration"/> to configure.</param>
        public static void UseTimers(this JobHostConfiguration config)
        {
            UseTimers(config, new TimersConfiguration());
        }

        /// <summary>
        /// Enables use of the Timer extensions
        /// </summary>
        /// <param name="config">The <see cref="JobHostConfiguration"/> to configure.</param>
        /// <param name="timersConfig">The <see cref="TimersConfiguration"/> to use.</param>
        public static void UseTimers(this JobHostConfiguration config, TimersConfiguration timersConfig)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            TimersExtensionConfig extensionConfig = new TimersExtensionConfig(timersConfig);

            IExtensionRegistry extensions = config.GetService<IExtensionRegistry>();
            extensions.RegisterExtension<IExtensionConfigProvider>(extensionConfig);
        }
    }
}
