﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Statiq.Common.Tracing
{
    /// <summary>
    /// Provides access to tracing functionality. This class is thread safe.
    /// </summary>
    public sealed class Trace : ITrace
    {
        private static readonly TraceSource TraceSource = new TraceSource("Statiq", SourceLevels.Information);
        private static readonly object ListenersLock = new object();

        private static int _indent = 0;

        public static ITrace Current { get; } = new Trace();

        private Trace()
        {
        }

        public static SourceLevels Level
        {
            get { return TraceSource.Switch.Level; }
            set { TraceSource.Switch.Level = value; }
        }

        public static void AddListener(TraceListener listener)
        {
            lock (ListenersLock)
            {
                TraceSource.Listeners.Add(listener);
                listener.IndentLevel = _indent;
            }
        }

        public static void RemoveListener(TraceListener listener)
        {
            lock (ListenersLock)
            {
                listener.IndentLevel = 0;
                TraceSource.Listeners.Remove(listener);
            }
        }

        public static IEnumerable<TraceListener> GetListeners()
        {
            lock (ListenersLock)
            {
                return TraceSource.Listeners.OfType<TraceListener>().ToArray();
            }
        }

        // Stops the application
        public static void Critical(string messageOrFormat, params object[] args) =>
            TraceEvent(TraceEventType.Critical, messageOrFormat, args);

        // Prevents expected behavior
        public static void Error(string messageOrFormat, params object[] args) =>
            TraceEvent(TraceEventType.Error, messageOrFormat, args);

        // Unexpected behavior that does not prevent expected behavior
        public static void Warning(string messageOrFormat, params object[] args) =>
            TraceEvent(TraceEventType.Warning, messageOrFormat, args);

        public static void Information(string messageOrFormat, params object[] args) =>
            TraceEvent(TraceEventType.Information, messageOrFormat, args);

        public static void Verbose(string messageOrFormat, params object[] args) =>
            TraceEvent(TraceEventType.Verbose, messageOrFormat, args);

        public static void TraceEvent(TraceEventType eventType, string messageOrFormat, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                TraceSource.TraceEvent(eventType, 0, messageOrFormat);
            }
            else
            {
                TraceSource.TraceEvent(eventType, 0, messageOrFormat, args);
            }
        }

        SourceLevels ITrace.Level
        {
            get { return Level; }
            set { Level = value; }
        }

        void ITrace.AddListener(TraceListener listener) => AddListener(listener);

        void ITrace.RemoveListener(TraceListener listener) => RemoveListener(listener);

        IEnumerable<TraceListener> ITrace.Listeners => GetListeners();

        void ITrace.Critical(string messageOrFormat, params object[] args) =>
            Critical(messageOrFormat, args);

        void ITrace.Error(string messageOrFormat, params object[] args) =>
            Error(messageOrFormat, args);

        void ITrace.Warning(string messageOrFormat, params object[] args) =>
            Warning(messageOrFormat, args);

        void ITrace.Information(string messageOrFormat, params object[] args) =>
            Information(messageOrFormat, args);

        void ITrace.Verbose(string messageOrFormat, params object[] args) =>
            Verbose(messageOrFormat, args);

        void ITrace.TraceEvent(TraceEventType eventType, string messageOrFormat, params object[] args) =>
            TraceEvent(eventType, messageOrFormat, args);
    }
}
