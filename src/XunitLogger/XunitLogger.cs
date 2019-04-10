﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit.Abstractions;

public static class XunitLogger
{
    static AsyncLocal<LoggingContext> asyncLocal = new AsyncLocal<LoggingContext>();
    
    #region writeRedirects
    static XunitLogger()
    {
        var listeners = Trace.Listeners;
        listeners.Clear();
        listeners.Add(new TraceListener());
        var writer = new TestWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
    }
    #endregion

    public static void Write(string value)
    {
        var context = GetContext();
        var builder = context.Builder;
        lock (builder)
        {
            builder.Append(value);
        }
    }

    public static IReadOnlyList<string> Logs => GetContext().LogMessages;

    public static void Write(char value)
    {
        var context = GetContext();
        var builder = context.Builder;
        lock (builder)
        {
            builder.Append(value);
        }
    }

    public static void WriteLine(string value = null)
    {
        var context = GetContext();
        var builder = context.Builder;

        string message;
        lock (builder)
        {
            builder.Append(value);
            message = builder.ToString();
            builder.Clear();
            context.LogMessages.Add(message);
        }

        context.TestOutput.WriteLine(message);
    }

    public static void Flush()
    {
        var context = GetContext();
        var builder = context.Builder;
        var testOutput = context.TestOutput;
        string message;
        lock (builder)
        {
            message = builder.ToString();
            context.LogMessages.Add(message);
            context.Flushed = true;
        }

        testOutput.WriteLine(message);
    }

    static LoggingContext GetContext()
    {
        var context = asyncLocal.Value;
        if (context != null)
        {
            return context;
        }
        throw new Exception("An attempt was made to write to Trace or Console, however no logging context found. Either XunitLogger.Register(ITestOutputHelper) needs to be called at test startup, or have the test inherit from XunitLoggingBase.");
    }

    public static void Register(ITestOutputHelper output)
    {
        Guard.AgainstNull(output, nameof(output));
        asyncLocal.Value = new LoggingContext(output);
    }
}