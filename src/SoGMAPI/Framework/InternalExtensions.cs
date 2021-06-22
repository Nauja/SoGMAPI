using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using SoGModdingAPI.Framework.Events;
using SoGModdingAPI.Framework.Reflection;

namespace SoGModdingAPI.Framework
{
    /// <summary>Provides extension methods for SMAPI's internal use.</summary>
    internal static class InternalExtensions
    {
        /****
        ** IMonitor
        ****/
        /// <summary>Log a message for the player or developer the first time it occurs.</summary>
        /// <param name="monitor">The monitor through which to log the message.</param>
        /// <param name="hash">The hash of logged messages.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log severity level.</param>
        public static void LogOnce(this IMonitor monitor, HashSet<string> hash, string message, LogLevel level = LogLevel.Trace)
        {
            if (!hash.Contains(message))
            {
                monitor.Log(message, level);
                hash.Add(message);
            }
        }

        /****
        ** IModMetadata
        ****/
        /// <summary>Log a message using the mod's monitor.</summary>
        /// <param name="metadata">The mod whose monitor to use.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log severity level.</param>
        public static void LogAsMod(this IModMetadata metadata, string message, LogLevel level = LogLevel.Trace)
        {
            metadata.Monitor.Log(message, level);
        }

        /****
        ** ManagedEvent
        ****/
        /// <summary>Raise the event using the default event args and notify all handlers.</summary>
        /// <typeparam name="TEventArgs">The event args type to construct.</typeparam>
        /// <param name="event">The event to raise.</param>
        public static void RaiseEmpty<TEventArgs>(this ManagedEvent<TEventArgs> @event) where TEventArgs : new()
        {
            @event.Raise(Singleton<TEventArgs>.Instance);
        }

        /****
        ** Exceptions
        ****/
        /// <summary>Get a string representation of an exception suitable for writing to the error log.</summary>
        /// <param name="exception">The error to summarize.</param>
        public static string GetLogSummary(this Exception exception)
        {
            switch (exception)
            {
                case TypeLoadException ex:
                    return $"Failed loading type '{ex.TypeName}': {exception}";

                case ReflectionTypeLoadException ex:
                    string summary = exception.ToString();
                    foreach (Exception childEx in ex.LoaderExceptions)
                        summary += $"\n\n{childEx.GetLogSummary()}";
                    return summary;

                default:
                    return exception.ToString();
            }
        }

        /// <summary>Get the lowest exception in an exception stack.</summary>
        /// <param name="exception">The exception from which to search.</param>
        public static Exception GetInnermostException(this Exception exception)
        {
            while (exception.InnerException != null)
                exception = exception.InnerException;
            return exception;
        }

        /****
        ** ReaderWriterLockSlim
        ****/
        /// <summary>Run code within a read lock.</summary>
        /// <param name="lock">The lock to set.</param>
        /// <param name="action">The action to perform.</param>
        public static void InReadLock(this ReaderWriterLockSlim @lock, Action action)
        {
            @lock.EnterReadLock();
            try
            {
                action();
            }
            finally
            {
                @lock.ExitReadLock();
            }
        }

        /// <summary>Run code within a read lock.</summary>
        /// <typeparam name="TReturn">The action's return value.</typeparam>
        /// <param name="lock">The lock to set.</param>
        /// <param name="action">The action to perform.</param>
        public static TReturn InReadLock<TReturn>(this ReaderWriterLockSlim @lock, Func<TReturn> action)
        {
            @lock.EnterReadLock();
            try
            {
                return action();
            }
            finally
            {
                @lock.ExitReadLock();
            }
        }

        /// <summary>Run code within a write lock.</summary>
        /// <param name="lock">The lock to set.</param>
        /// <param name="action">The action to perform.</param>
        public static void InWriteLock(this ReaderWriterLockSlim @lock, Action action)
        {
            @lock.EnterWriteLock();
            try
            {
                action();
            }
            finally
            {
                @lock.ExitWriteLock();
            }
        }

        /// <summary>Run code within a write lock.</summary>
        /// <typeparam name="TReturn">The action's return value.</typeparam>
        /// <param name="lock">The lock to set.</param>
        /// <param name="action">The action to perform.</param>
        public static TReturn InWriteLock<TReturn>(this ReaderWriterLockSlim @lock, Func<TReturn> action)
        {
            @lock.EnterWriteLock();
            try
            {
                return action();
            }
            finally
            {
                @lock.ExitWriteLock();
            }
        }
    }
}
