using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.Meta;
using Statiq.Common.Modules;

namespace Statiq.Core.Modules.Metadata
{
    /// <summary>
    /// Tests metadata for existence, typing, and supplied assertions.
    /// </summary>
    /// <typeparam name="T">The type of the metadata value to convert to for validation.</typeparam>
    /// <remarks>
    /// This module performs tests on metadata. It can ensure metadata exists, that it can be converted to the correct type, and that is passes
    /// arbitrary tests (delegates) to ensure validity. Metadata can be specified as optional, in which case, typing and assertion testing
    /// will only be run if the metadata exists. If any check fails, this module throws an exception with a descriptive error message then
    /// halts further execution.
    /// </remarks>
    /// <example>
    /// This example will ensure "Title" exists. (It will also perform a type check, but since "object" matches anything, the type check will
    /// always succeed.)
    /// <code>
    /// ValidateMeta&lt;object&gt;("Title")
    /// </code>
    /// </example>
    /// <example>
    /// This example will ensure that if "Date" exists, it can convert to a valid DateTime.
    /// <code>
    /// ValidateMeta&lt;DateTime&gt;("Date")
    ///    .IsOptional()
    /// </code>
    /// </example>
    /// <example>
    /// This example will ensure "Age" (1) exists, (2) can convert to an integer, (3) and is greater than 0 and less than 121.
    /// If it fails any assertion, the provided error message will be output. (In this case, those two assertions could be rolled
    /// into one, but then they would share an error message. Separate assertions allow more specific error messages.) Assertions will
    /// be checked in order. Any assertion can assume all previous assertions have passed. Error messages will be appended with
    /// the document Source and Id properties to assist in identifying invalid documents.
    /// <code>
    /// ValidateMeta&lt;int&gt;("Age")
    ///    .WithAssertion(a =&gt; a &gt; 0, "You have to be born.")
    ///    .WithAssertion(a =&gt; a &lt;= 120, "You are way, way too old.")
    /// </code>
    /// </example>
    /// <category>Metadata</category>
    public class ValidateMeta<T> : IModule
    {
        private readonly string _key;
        private readonly List<Assertion<T>> _assertions;
        private bool _optional;

        /// <summary>
        /// Performs validation checks on metadata.
        /// </summary>
        /// <param name="key">The meta key representing the value to test.</param>
        public ValidateMeta(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            _key = key;
            _assertions = new List<Assertion<T>>();
        }

        /// <summary>
        /// Declares the entire check as optional. Is this is set, and the meta key doesn't exist, no checks will be run.
        /// </summary>
        /// <returns>The current module instance.</returns>
        public ValidateMeta<T> IsOptional()
        {
            _optional = true;
            return this;
        }

        /// <summary>
        /// Performs validation checks on metadata.
        /// </summary>
        /// <param name="execute">The assertion function, of type Func&lt;T, bool&gt; where T is the generic parameter of the ValidateMeta
        /// declaration. Assertions are strongly-typed and can assume the value has been converted to the correct type. If the function returns
        /// false, the check failed, an exception will be thrown, and execution will halt.</param>
        /// <param name="message">The error message to output on failure.</param>
        /// <returns>The current module instance.</returns>
        public ValidateMeta<T> WithAssertion(Func<T, bool> execute, string message = null)
        {
            _assertions.Add(new Assertion<T>(execute, message));
            return this;
        }

        /// <inheritdoc />
        public Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            Parallel.ForEach(inputs, input =>
            {
                // Check if the key exists
                if (!input.ContainsKey(_key))
                {
                    if (_optional)
                    {
                        // It doesn't exist, but it wasn't required, so we're good.
                        return;
                    }

                    // This doesn't exist, and was required.
                    throw GetException($"Meta key \"{_key}\" is not found.");
                }

                // Attempt to convert it to the desired type
                if (!input.TryGetValue(_key, out T value))
                {
                    // Report the original string, as the value coming out of TryGetValue might not be the same as what went in.
                    throw GetException($"Value \"{input.String(_key)}\" could not be converted to type \"{typeof(T).Name}\".");
                }

                // Check each assertion
                foreach (Assertion<T> assertion in _assertions)
                {
                    if (!assertion.Execute(value))
                    {
                        throw GetException(assertion.Message);
                    }
                }
            });

            return Task.FromResult<IEnumerable<IDocument>>(inputs);
        }

        private Exception GetException(string message) => new Exception($"{message ?? "Assertion failed"}");
    }
}
