﻿using System;
using System.Diagnostics;
using System.Text;

namespace Statiq.Core.Tracing
{
    // This routes trace and debug messages from the Trace/Debug classes to the Statiq Trace TraceSource
    internal class DiagnosticsTraceListener : TraceListener
    {
        public override void Write(string message)
        {
            Statiq.Common.Tracing.Trace.Verbose(message);
        }

        public override void WriteLine(string message)
        {
            Statiq.Common.Tracing.Trace.Verbose(message);
        }

        public override void Fail(string message)
        {
            Statiq.Common.Tracing.Trace.Error(message);
        }

        public override void Fail(string message, string detailMessage)
        {
            Statiq.Common.Tracing.Trace.Error(message + " " + detailMessage);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            TraceData(eventCache, source, eventType, id, new object[] { data });
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; ++i)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append("{");
                sb.Append(i);
                sb.Append("}");
            }

            Statiq.Common.Tracing.Trace.Verbose(sb.ToString());
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            Statiq.Common.Tracing.Trace.TraceEvent(eventType, id.ToString());
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            Statiq.Common.Tracing.Trace.TraceEvent(eventType, format, args);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            Statiq.Common.Tracing.Trace.TraceEvent(eventType, message);
        }

        public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
        {
            Statiq.Common.Tracing.Trace.Verbose(message);
        }
    }
}
