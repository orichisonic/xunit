﻿#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using Xunit.Abstractions;

namespace Xunit
{
    static class Xunit1ExceptionUtility
    {
        static readonly Regex NestedMessagesRegex = new Regex(@"-*\s*((?<type>.*?) :\s*)?(?<message>.+?)((\r?\n-)|\z)", RegexOptions.ExplicitCapture | RegexOptions.Multiline | RegexOptions.Singleline);
        static readonly Regex NestedStackTracesRegex = new Regex(@"\r?\n----- Inner Stack Trace -----\r?\n", RegexOptions.Compiled);

        public static IFailureInformation ConvertToFailureInformation(Exception? exception)
        {
            var exceptionTypes = new List<string?>();
            var messages = new List<string>();
            var stackTraces = new List<string?>();
            var indices = new List<int>();
            var parentIndex = -1;

            while (exception != null)
            {
                var stackTrace = exception.StackTrace;
                var rethrowIndex = stackTrace == null ? -1 : stackTrace.IndexOf("$$RethrowMarker$$", StringComparison.Ordinal);
                if (rethrowIndex > -1)
                    stackTrace = stackTrace!.Substring(0, rethrowIndex);

                exceptionTypes.Add(exception.GetType().FullName);
                messages.Add(exception.Message);
                stackTraces.Add(stackTrace);
                indices.Add(parentIndex);

                parentIndex++;
                exception = exception.InnerException;
            }

            return new FailureInformation(
                exceptionTypes.ToArray(),
                messages.ToArray(),
                stackTraces.ToArray(),
                indices.ToArray()
            );
        }

        public static IFailureInformation ConvertToFailureInformation(XmlNode failureNode)
        {
            Guard.ArgumentNotNull(nameof(failureNode), failureNode);

            var exceptionTypeAttribute = failureNode.Attributes["exception-type"];
            var exceptionType = exceptionTypeAttribute != null ? exceptionTypeAttribute.Value : string.Empty;
            var message = failureNode.SelectSingleNode("message").InnerText;
            var stackTraceNode = failureNode.SelectSingleNode("stack-trace");
            var stackTrace = stackTraceNode == null ? string.Empty : stackTraceNode.InnerText;

            return ConvertToFailureInformation(exceptionType, message, stackTrace);
        }

        static IFailureInformation ConvertToFailureInformation(string outermostExceptionType, string nestedExceptionMessages, string nestedStackTraces)
        {
            var exceptionTypes = new List<string?>();
            var messages = new List<string>();

            var match = NestedMessagesRegex.Match(nestedExceptionMessages);
            for (var i = 0; match.Success; i++, match = match.NextMatch())
            {
                exceptionTypes.Add(match.Groups["type"].Value);
                messages.Add(match.Groups["message"].Value);
            }

            if (exceptionTypes.Count > 0 && exceptionTypes[0] == "")
                exceptionTypes[0] = outermostExceptionType;

            var stackTraces = NestedStackTracesRegex.Split(nestedStackTraces);
            var exceptionParentIndices = new int[stackTraces.Length];
            for (int i = 0; i < exceptionParentIndices.Length; i++)
                exceptionParentIndices[i] = i - 1;

            return new FailureInformation(
                exceptionTypes.ToArray(),
                messages.ToArray(),
                stackTraces,
                exceptionParentIndices
            );
        }

        class FailureInformation : IFailureInformation
        {
            public FailureInformation(
                string?[] exceptionTypes,
                string[] messages,
                string?[] stackTraces,
                int[] exceptionParentIndices)
            {
                ExceptionTypes = exceptionTypes;
                Messages = messages;
                StackTraces = stackTraces;
                ExceptionParentIndices = exceptionParentIndices;
            }

            public string?[] ExceptionTypes { get; set; }
            public string[] Messages { get; set; }
            public string?[] StackTraces { get; set; }
            public int[] ExceptionParentIndices { get; set; }
        }
    }
}

#endif
