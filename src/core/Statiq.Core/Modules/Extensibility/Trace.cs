﻿using System.Diagnostics;
using System.Threading.Tasks;
using Statiq.Common.Configuration;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.Modules;

namespace Statiq.Core.Modules.Extensibility
{
    /// <summary>
    /// Outputs trace messages during execution.
    /// </summary>
    /// <remarks>
    /// This module has no effect on documents and the input documents are passed through to output documents.
    /// </remarks>
    /// <category>Extensibility</category>
    public class Trace : ContentModule
    {
        private TraceEventType _traceEventType = TraceEventType.Information;

        /// <summary>
        /// Outputs the string value of the returned object to trace. This allows
        /// you to trace different content for each document depending on the input document.
        /// </summary>
        /// <param name="content">A delegate that returns the content to trace.</param>
        public Trace(DocumentConfig<string> content)
            : base(content)
        {
        }

        /// <summary>
        /// The specified modules are executed against an empty initial document and the
        /// resulting document content is output to trace.
        /// </summary>
        /// <param name="modules">The modules to execute.</param>
        public Trace(params IModule[] modules)
            : base(modules)
        {
        }

        /// <summary>
        /// Sets the event type to trace.
        /// </summary>
        /// <param name="traceEventType">The event type to trace.</param>
        /// <returns>The current module instance.</returns>
        public Trace EventType(TraceEventType traceEventType)
        {
            _traceEventType = traceEventType;
            return this;
        }

        /// <inheritdoc />
        protected override Task<IDocument> ExecuteAsync(string content, IDocument input, IExecutionContext context)
        {
            Common.Tracing.Trace.TraceEvent(_traceEventType, content);
            return Task.FromResult(input);
        }
    }
}
