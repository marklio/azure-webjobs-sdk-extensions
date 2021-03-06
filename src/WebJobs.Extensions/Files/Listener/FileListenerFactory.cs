﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Files;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.WebJobs.Files.Listeners
{
    internal class FileListenerFactory : IListenerFactory
    {
        private readonly FileTriggerAttribute _attribute;
        private readonly ITriggeredFunctionExecutor<FileSystemEventArgs> _executor;
        private readonly FilesConfiguration _config;

        public FileListenerFactory(FilesConfiguration config, FileTriggerAttribute attribute, ITriggeredFunctionExecutor<FileSystemEventArgs> executor)
        {
            _config = config;
            _attribute = attribute;
            _executor = executor;
        }

        public Task<IListener> CreateAsync(ListenerFactoryContext context)
        {
            FileListener listener = new FileListener(_config, _attribute, _executor);
            return Task.FromResult<IListener>(listener);
        }
    }
}
