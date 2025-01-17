﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConcurrentCollections;
using Microsoft.Extensions.DependencyInjection;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.IO;
using Statiq.Common.Modules;
using Statiq.Common.Tracing;
using Statiq.Core.Modules.Control;

namespace Statiq.Core.Execution
{
    /// <summary>
    /// This contains the pipeline execution context and other data
    /// needed to execute a pipeline and cache it's results.
    /// </summary>
    internal class PipelinePhase : IDisposable
    {
        private readonly IList<IModule> _modules;
        private bool _disposed;

        public PipelinePhase(IPipeline pipeline, string pipelineName, Phase phase, IList<IModule> modules, params PipelinePhase[] dependencies)
        {
            Pipeline = pipeline;
            PipelineName = pipelineName;
            Phase = phase;
            _modules = modules ?? new List<IModule>();
            Dependencies = dependencies ?? Array.Empty<PipelinePhase>();
        }

        public IPipeline Pipeline { get; }

        public string PipelineName { get; }

        public Phase Phase { get; }

        /// <summary>
        /// Contains direct dependencies for this pipeline and phase.
        /// The first dependency should contain the input documents for this phase.
        /// </summary>
        public PipelinePhase[] Dependencies { get; set; }

        /// <summary>
        /// Holds the output documents from the previous execution of this phase.
        /// </summary>
        public ImmutableArray<IDocument> OutputDocuments { get; private set; } = ImmutableArray<IDocument>.Empty;

        /// <summary>
        /// The first dependency always holds the input documents for this phase.
        /// </summary>
        /// <returns>The input documents for this phase.</returns>
        private ImmutableArray<IDocument> GetInputDocuments() => Dependencies.Length == 0 ? ImmutableArray<IDocument>.Empty : Dependencies[0].OutputDocuments;

        // This is the main execute method called by the engine
        public async Task ExecuteAsync(
            Engine engine,
            Guid executionId,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PipelinePhase));
            }

            if (_modules.Count == 0)
            {
                Trace.Verbose($"Pipeline {PipelineName}/{Phase} contains no modules, skipping");
                OutputDocuments = GetInputDocuments();
                return;
            }

            System.Diagnostics.Stopwatch pipelineStopwatch = System.Diagnostics.Stopwatch.StartNew();
            Trace.Verbose($"Executing pipeline {PipelineName}/{Phase} with {_modules.Count} module(s)");
            try
            {
                // Execute all modules in the pipeline with a new DI scope per phase
                IServiceScopeFactory serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                using (IServiceScope serviceScope = serviceScopeFactory.CreateScope())
                {
                    ExecutionContext context = new ExecutionContext(engine, executionId, this, serviceScope.ServiceProvider, cancellationToken);
                    OutputDocuments = await Engine.ExecuteAsync(context, _modules, GetInputDocuments());
                    pipelineStopwatch.Stop();
                    Trace.Information($"Executed pipeline {PipelineName}/{Phase} in {pipelineStopwatch.ElapsedMilliseconds} ms resulting in {OutputDocuments.Length} output document(s)");
                }
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    Trace.Error($"Error while executing pipeline {PipelineName}/{Phase}");
                }
                OutputDocuments = ImmutableArray<IDocument>.Empty;
                throw;
            }

            // Store the result documents, but only if this is the Process phase of a non-isolated pipeline
            if (!Pipeline.Isolated && Phase == Phase.Process)
            {
                engine.Documents.AddOrUpdate(
                    PipelineName,
                    OutputDocuments,
                    (_, __) => OutputDocuments);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Pipeline));
            }
            _disposed = true;

            DisposeModules(_modules);
        }

        private static void DisposeModules(IEnumerable<IModule> modules)
        {
            foreach (IModule module in modules)
            {
                (module as IDisposable)?.Dispose();
                if (module is IEnumerable<IModule> childModules)
                {
                    DisposeModules(childModules);
                }
            }
        }
    }
}
