using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;

namespace StravaUpload.StravaUploadFunction
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class TraceWriterLogger : ILogger
    {
        private readonly TraceWriter traceWriter;

        public TraceWriterLogger(TraceWriter traceWriter)
        {
            this.traceWriter = traceWriter;
        }

        public void Error(string message)
        {
            this.traceWriter.Error(message);
        }

        public void Information(string message)
        {
            this.traceWriter.Info(message);
        }

        public void Warning(string message)
        {
            this.traceWriter.Warning(message);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Information:
                    this.traceWriter.Info(state.ToString());
                    break;

                case LogLevel.Warning:
                    this.traceWriter.Warning(state.ToString());
                    break;

                case LogLevel.Error:
                    this.traceWriter.Error(state.ToString());
                    break;

                case LogLevel.Debug:
                    this.traceWriter.Verbose(state.ToString());
                    break;

                case LogLevel.Critical:
                    this.traceWriter.Verbose(state.ToString());
                    break;

                case LogLevel.Trace:
                    this.traceWriter.Verbose(state.ToString());
                    break;

                default:
                    this.traceWriter.Info(state.ToString());
                    break;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class TraceWriterLogger<T> : ILogger<T>
    {
        private readonly TraceWriter traceWriter;

        public TraceWriterLogger(TraceWriter traceWriter)
        {
            this.traceWriter = traceWriter;
        }

        public void Error(string message)
        {
            this.traceWriter.Error(message);
        }

        public void Information(string message)
        {
            this.traceWriter.Info(message);
        }

        public void Warning(string message)
        {
            this.traceWriter.Warning(message);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Information:
                    this.traceWriter.Info(state.ToString());
                    break;

                case LogLevel.Warning:
                    this.traceWriter.Warning(state.ToString());
                    break;

                case LogLevel.Error:
                    this.traceWriter.Error(state.ToString());
                    break;

                case LogLevel.Debug:
                    this.traceWriter.Verbose(state.ToString());
                    break;

                case LogLevel.Critical:
                    this.traceWriter.Verbose(state.ToString());
                    break;

                case LogLevel.Trace:
                    this.traceWriter.Verbose(state.ToString());
                    break;

                default:
                    this.traceWriter.Info(state.ToString());
                    break;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }
    }
}