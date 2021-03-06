﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Files;
using Microsoft.Azure.WebJobs.Extensions.Files.Listener;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Microsoft.Azure.WebJobs.Files.Listeners
{
    internal sealed class FileListener : IListener
    {
        private readonly FileTriggerAttribute _attribute;
        private readonly ITriggeredFunctionExecutor<FileSystemEventArgs> _triggerExecutor;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly FilesConfiguration _config;
        private FileProcessor _processor;
        private System.Timers.Timer _cleanupTimer;
        private Random _rand = new Random();

        private FileSystemWatcher _watcher;
        private bool _disposed;

        public FileListener(FilesConfiguration config, FileTriggerAttribute attribute, ITriggeredFunctionExecutor<FileSystemEventArgs> triggerExecutor)
        {
            _config = config;
            _attribute = attribute;
            _triggerExecutor = triggerExecutor;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public FileProcessor Processor
        {
            get
            {
                return _processor;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_watcher != null && _watcher.EnableRaisingEvents)
            {
                throw new InvalidOperationException("The listener has already been started.");
            }

            if (string.IsNullOrEmpty(_config.RootPath) || !Directory.Exists(_config.RootPath))
            {
                throw new InvalidOperationException("FilesConfiguration.RootPath must be set to a valid directory location.");
            }

            CreateFileWatcher();

            FileProcessorFactoryContext context = new FileProcessorFactoryContext(_config, _attribute, _triggerExecutor);
            _processor = _config.ProcessorFactory.CreateFileProcessor(context, _cancellationTokenSource);

            // on startup, process any preexisting files that haven't been processed yet
            _processor.ProcessFiles();

            // Create a timer to cleanup processed files.
            // The timer doesn't auto-reset. It will reset itself
            // when it receives file events
            _cleanupTimer = new System.Timers.Timer()
            {
                AutoReset = false,
                Interval = _rand.Next(5 * 1000, 8 * 1000)
            };
            _cleanupTimer.Elapsed += OnCleanupTimer;
            _cleanupTimer.Start();

            await Task.FromResult<bool>(true);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_watcher == null || !_watcher.EnableRaisingEvents)
            {
                throw new InvalidOperationException("The listener has not yet been started or has already been stopped.");
            }
 
            // Signal ProcessMessage to shut down gracefully
            _cancellationTokenSource.Cancel();

            _watcher.EnableRaisingEvents = false;

            _cleanupTimer.Stop();
            _cleanupTimer.Dispose();
            _cleanupTimer = null;

            return Task.FromResult<bool>(true);
        }

        public void Cancel()
        {
            ThrowIfDisposed();
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Running callers might still be using the cancellation token.
                // Mark it canceled but don't dispose of the source while the callers are running.
                // Otherwise, callers would receive ObjectDisposedException when calling token.Register.
                // For now, rely on finalization to clean up _cancellationTokenSource's wait handle (if allocated).
                _cancellationTokenSource.Cancel();

                if (_watcher != null)
                {
                    _watcher.Dispose();
                    _watcher = null;
                }

                if (_cleanupTimer != null)
                {
                    _cleanupTimer.Stop();
                    _cleanupTimer.Dispose();
                    _cleanupTimer = null;
                }

                _disposed = true;
            }
        }

        private void CreateFileWatcher()
        {
            string watchPath = Path.Combine(_config.RootPath, _attribute.GetNormalizedPath());

            if (!Directory.Exists(watchPath))
            {
                throw new InvalidOperationException(string.Format("Path '{0}' does not exist.", watchPath));
            }

            _watcher = new FileSystemWatcher
            {
                Path = watchPath,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                Filter = _attribute.Filter
            };

            if ((_attribute.ChangeTypes & WatcherChangeTypes.Changed) != 0)
            {
                _watcher.Changed += new FileSystemEventHandler(FileChangeHandler);
            }

            if ((_attribute.ChangeTypes & WatcherChangeTypes.Created) != 0)
            {
                _watcher.Created += new FileSystemEventHandler(FileChangeHandler);
            }

            if ((_attribute.ChangeTypes & WatcherChangeTypes.Deleted) != 0)
            {
                _watcher.Deleted += new FileSystemEventHandler(FileChangeHandler);
            }

            if ((_attribute.ChangeTypes & WatcherChangeTypes.Renamed) != 0)
            {
                _watcher.Renamed += new RenamedEventHandler(FileChangeHandler);
            }

            _watcher.EnableRaisingEvents = true;
        }

        private void OnCleanupTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            _processor.Cleanup();
        }

        private void FileChangeHandler(object source, FileSystemEventArgs e)
        {
            _processor.ProcessFileAsync(e).Wait();

            // when we receive file events, reset the cleanup timer
            _cleanupTimer.Enabled = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }
        }
    }
}
