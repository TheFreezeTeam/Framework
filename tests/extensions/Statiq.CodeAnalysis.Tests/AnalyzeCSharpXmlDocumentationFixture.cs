﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.Meta;
using Statiq.Common.Modules;
using Statiq.Testing.Documents;
using Statiq.Testing.Execution;

namespace Statiq.CodeAnalysis.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class AnalyzeCSharpXmlDocumentationFixture : AnalyzeCSharpBaseFixture
    {
        public class ExecuteTests : AnalyzeCSharpXmlDocumentationFixture
        {
            [Test]
            public async Task SingleLineSummary()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Green
                        {
                        }

                        /// <summary>This is another summary.</summary>
                        struct Red
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("This is a summary.", GetResult(results, "Green")["Summary"]);
                Assert.AreEqual("This is another summary.", GetResult(results, "Red")["Summary"]);
            }

            [Test]
            public async Task MultiLineSummary()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// </summary>
                        class Green
                        {
                        }

                        /// <summary>
                        /// This is
                        /// another summary.
                        /// </summary>
                        struct Red
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    This is a summary.\n    ", GetResult(results, "Green")["Summary"]);
                Assert.AreEqual("\n    This is\n    another summary.\n    ", GetResult(results, "Red")["Summary"]);
            }

            [Test]
            public async Task MultipleSummaryElements()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        /// <summary>This is another summary.</summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("This is a summary.\nThis is another summary.", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task NoSummary()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(string.Empty, GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithCElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c>some code</c> in a summary.
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    This is <code>some code</code> in a summary.\n    ", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithCElementAndInlineCssClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c class=""code"">some code</c> in a summary.
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    This is <code class=\"code\">some code</code> in a summary.\n    ", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithCElementAndDeclaredCssClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c>some code</c> in a summary.
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp().WithCssClasses("code", "code");

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    This is <code class=\"code\">some code</code> in a summary.\n    ", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithCElementAndInlineAndDeclaredCssClasses()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c class=""code"">some code</c> in a summary.
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp().WithCssClasses("code", "more-code");

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    This is <code class=\"code more-code\">some code</code> in a summary.\n    ", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithMultipleCElements()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c>some code</c> in <c>a</c> summary.
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    This is <code>some code</code> in <code>a</code> summary.\n    ", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithCodeElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is
                        /// <code>
                        /// with some code
                        /// </code>
                        /// a summary
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    This is\n    <pre><code>with some code</code></pre>\n    a summary\n    ", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithCodeElementAndCElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is <c>some code</c> and
                        /// <code>
                        /// with some code
                        /// </code>
                        /// a summary
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    "\n    This is <code>some code</code> and\n    <pre><code>with some code</code></pre>\n    a summary\n    ",
                    GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithMultipleCodeElements()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is
                        /// <code>
                        /// with some code
                        /// </code>
                        /// a summary
                        /// <code>
                        /// more code
                        /// </code>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    This is\n    <pre><code>with some code</code></pre>\n    a summary\n    <pre><code>more code</code></pre>\n    ", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryOnPartialClasses()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary repeated for each partial class
                        /// </summary>
                        partial class Green
                        {
                        }

                        /// <summary>
                        /// This is a summary repeated for each partial class
                        /// </summary>
                        partial class Green
                        {
                        }

                        /// <summary>
                        /// This is a summary repeated for each partial class
                        /// </summary>
                        partial class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    This is a summary repeated for each partial class\n    ", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task MethodWithParam()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <param name=""bar"">comment</param>
                            void Go(string bar)
                            {
                            }
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    "bar",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Params")[0].Name);
                Assert.AreEqual(
                    "comment",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Params")[0].Html);
            }

            [Test]
            public async Task MethodWithMissingParam()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <param name=""bar"">comment</param>
                            void Go()
                            {
                            }
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                CollectionAssert.IsEmpty(GetMember(results, "Green", "Go").List<ReferenceComment>("Params"));
            }

            [Test]
            public async Task MethodWithExceptionElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <exception cref=""FooException"">Throws when null</exception>
                            void Go()
                            {
                            }
                        }

                        class FooException : Exception
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    "FooException",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[0].Name);
                Assert.AreEqual(
                    "<code><a href=\"/Foo/FooException/index.html\">FooException</a></code>",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[0].Link);
                Assert.AreEqual(
                    "Throws when null",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[0].Html);
            }

            [Test]
            public async Task MethodWithUnknownExceptionElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <exception cref=""FooException"">Throws when null</exception>
                            void Go()
                            {
                            }
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    "FooException",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[0].Name);
                Assert.AreEqual(
                    "FooException",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[0].Link);
                Assert.AreEqual(
                    "Throws when null",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[0].Html);
            }

            [Test]
            public async Task ExceptionElementWithoutCref()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <exception>Throws when null</exception>
                            void Go()
                            {
                            }
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    string.Empty,
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[0].Name);
                Assert.AreEqual(
                    "Throws when null",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[0].Html);
            }

            [Test]
            public async Task MultipleExceptionElements()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <exception cref=""FooException"">Throws when null</exception>
                            /// <exception cref=""BarException"">Throws for another reason</exception>
                            void Go()
                            {
                            }
                        }

                        class FooException : Exception
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(2, GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions").Count);
                Assert.AreEqual(
                    "<code><a href=\"/Foo/FooException/index.html\">FooException</a></code>",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[0].Link);
                Assert.AreEqual(
                    "FooException",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[0].Name);
                Assert.AreEqual(
                    "Throws when null",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[0].Html);
                Assert.AreEqual(
                    "BarException",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[1].Link);
                Assert.AreEqual(
                    "BarException",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[1].Name);
                Assert.AreEqual(
                    "Throws for another reason",
                    GetMember(results, "Green", "Go").List<ReferenceComment>("Exceptions")[1].Html);
            }

            [Test]
            public async Task SummaryWithBulletListElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// <list type=""bullet"">
                        /// <listheader>
                        /// <term>A</term>
                        /// <description>a</description>
                        /// </listheader>
                        /// <item>
                        /// <term>X</term>
                        /// <description>x</description>
                        /// </item>
                        /// <item>
                        /// <term>Y</term>
                        /// <description>y</description>
                        /// </item>
                        /// </list>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    @"
                This is a summary.
                <ul>
                <li>
                <span class=""term"">A</span>
                <span class=""description"">a</span>
                </li>
                <li>
                <span class=""term"">X</span>
                <span class=""description"">x</span>
                </li>
                <li>
                <span class=""term"">Y</span>
                <span class=""description"">y</span>
                </li>
                </ul>
                ".Replace("\r\n", "\n").Replace("                ", "    "),
                    GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithNumberListElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// <list type=""number"">
                        /// <listheader>
                        /// <term>A</term>
                        /// <description>a</description>
                        /// </listheader>
                        /// <item>
                        /// <term>X</term>
                        /// <description>x</description>
                        /// </item>
                        /// <item>
                        /// <term>Y</term>
                        /// <description>y</description>
                        /// </item>
                        /// </list>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    @"
                This is a summary.
                <ol>
                <li>
                <span class=""term"">A</span>
                <span class=""description"">a</span>
                </li>
                <li>
                <span class=""term"">X</span>
                <span class=""description"">x</span>
                </li>
                <li>
                <span class=""term"">Y</span>
                <span class=""description"">y</span>
                </li>
                </ol>
                ".Replace("\r\n", "\n").Replace("                ", "    "),
                    GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithTableListElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// This is a summary.
                        /// <list type=""table"">
                        /// <listheader>
                        /// <term>A</term>
                        /// <term>a</term>
                        /// </listheader>
                        /// <item>
                        /// <term>X</term>
                        /// <term>x</term>
                        /// </item>
                        /// <item>
                        /// <term>Y</term>
                        /// <term>y</term>
                        /// </item>
                        /// </list>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    @"
                This is a summary.
                <table class=""table"">
                <tr>
                <th>A</th>
                <th>a</th>
                </tr>
                <tr>
                <td>X</td>
                <td>x</td>
                </tr>
                <tr>
                <td>Y</td>
                <td>y</td>
                </tr>
                </table>
                ".Replace("\r\n", "\n").Replace("                ", "    "),
                    GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithParaElements()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// <para>ABC</para>
                        /// <para>XYZ</para>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    <p>ABC</p>\n    <p>XYZ</p>\n    ", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithParaElementsAndNestedCElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// <para>ABC</para>
                        /// <para>X<c>Y</c>Z</para>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    <p>ABC</p>\n    <p>X<code>Y</code>Z</p>\n    ", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithSeeElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Check <see cref=""Red""/> class</summary>
                        class Green
                        {
                        }

                        class Red
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Check <code><a href=\"/Foo/Red/index.html\">Red</a></code> class", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithSeeElementWithNotFoundSymbol()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Check <see cref=""Blue""/> class</summary>
                        class Green
                        {
                        }

                        class Red
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Check <code>Blue</code> class", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithSeeElementWithNonCompilationGenericSymbol()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Check <see cref=""IEnumerable{string}""/> class</summary>
                        class Green
                        {
                        }

                        class Red
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Check <code>IEnumerable&lt;string&gt;</code> class", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithSeeElementToMethod()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Check <see cref=""Red.Blue""/> method</summary>
                        class Green
                        {
                        }

                        class Red
                        {
                            void Blue()
                            {
                            }
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Check <code><a href=\"/Foo/Red/00F22A50.html\">Blue()</a></code> method", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithUnknownSeeElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Check <see cref=""Red""/> class</summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Check <code>Red</code> class", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task SummaryWithSeealsoElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Check this out <seealso cref=""Red""/></summary>
                        class Green
                        {
                        }

                        class Red
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                // <seealso> should be removed from the summary and instead placed in the SeeAlso metadata
                Assert.AreEqual("Check this out ", GetResult(results, "Green")["Summary"]);
                Assert.AreEqual("<code><a href=\"/Foo/Red/index.html\">Red</a></code>", GetResult(results, "Green").Get<IReadOnlyList<string>>("SeeAlso")[0]);
            }

            [Test]
            public async Task RootSeealsoElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <seealso cref=""Red""/>
                        class Green
                        {
                        }

                        class Red
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("<code><a href=\"/Foo/Red/index.html\">Red</a></code>", GetResult(results, "Green").Get<IReadOnlyList<string>>("SeeAlso")[0]);
            }

            [Test]
            public async Task OtherCommentWithSeeElement()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <bar>Check <see cref=""Red""/> class</bar>
                        class Green
                        {
                        }

                        class Red
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    "Check <code><a href=\"/Foo/Red/index.html\">Red</a></code> class",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[0].Html);
            }

            [Test]
            public async Task MultipleOtherComments()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <bar>Circle</bar>
                        /// <bar>Square</bar>
                        /// <bar>Rectangle</bar>
                        class Green
                        {
                        }

                        class Red
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    3,
                    GetResult(results, "Green").List<OtherComment>("BarComments").Count);
                Assert.AreEqual(
                    "Circle",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[0].Html);
                Assert.AreEqual(
                    "Square",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[1].Html);
                Assert.AreEqual(
                    "Rectangle",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[2].Html);
            }

            [Test]
            public async Task OtherCommentsWithAttributes()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <bar a='x'>Circle</bar>
                        /// <bar a='y' b='z'>Square</bar>
                        class Green
                        {
                        }

                        class Red
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    1,
                    GetResult(results, "Green").List<OtherComment>("BarComments")[0].Attributes.Count);
                Assert.AreEqual(
                    "x",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[0].Attributes["a"]);
                Assert.AreEqual(
                    2,
                    GetResult(results, "Green").List<OtherComment>("BarComments")[1].Attributes.Count);
                Assert.AreEqual(
                    "y",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[1].Attributes["a"]);
                Assert.AreEqual(
                    "z",
                    GetResult(results, "Green").List<OtherComment>("BarComments")[1].Attributes["b"]);
            }

            [Test]
            public async Task NoDocsForImplicitSymbols()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <summary>This is a summary.</summary>
                            Green() {}
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp()
                    .WhereSymbol(x => x is INamedTypeSymbol);

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.IsFalse(GetResult(results, "Green").Get<IReadOnlyList<IDocument>>("Constructors")[0].ContainsKey("Summary"));
            }

            [Test]
            public async Task WithDocsForImplicitSymbols()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            /// <summary>This is a summary.</summary>
                            Green() {}
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp()
                    .WhereSymbol(x => x is INamedTypeSymbol)
                    .WithDocsForImplicitSymbols();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("This is a summary.", GetResult(results, "Green").Get<IReadOnlyList<IDocument>>("Constructors")[0]["Summary"]);
            }

            [Test]
            public async Task ExternalInclude()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <include file=""Included.xml"" path=""//Test/*"" />
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("This is a included summary.", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task NamespaceSummary()
            {
                // Given
                const string code = @"
                    /// <summary>This is a summary.</summary>
                    namespace Foo
                    {
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("This is a summary.", GetResult(results, "Foo")["Summary"]);
            }

            [Test]
            public async Task NamespaceSummaryWithNamespaceDocClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                        }

                        /// <summary>This is a summary.</summary>
                        class NamespaceDoc
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("This is a summary.", GetResult(results, "Foo")["Summary"]);
            }

            [Test]
            public async Task InheritFromBaseClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Green
                        {
                        }

                        /// <inheritdoc />
                        class Blue : Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("This is a summary.", GetResult(results, "Blue")["Summary"]);
            }

            [Test]
            public async Task ImplicitInheritFromBaseClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Green
                        {
                        }

                        class Blue : Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp().WithImplicitInheritDoc();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("This is a summary.", GetResult(results, "Blue")["Summary"]);
            }

            [Test]
            public async Task InheritFromCref()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Green
                        {
                        }

                        /// <inheritdoc cref=""Green"" />
                        class Blue
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("This is a summary.", GetResult(results, "Blue")["Summary"]);
            }

            [Test]
            public async Task CircularInheritdoc()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        /// <inheritdoc cref=""Blue"" />
                        class Green
                        {
                        }

                        /// <inheritdoc cref=""Green"" />
                        class Blue
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("This is a summary.", GetResult(results, "Blue")["Summary"]);
            }

            [Test]
            public async Task RecursiveInheritdoc()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Red
                        {
                        }

                        /// <inheritdoc cref=""Red"" />
                        class Green
                        {
                        }

                        /// <inheritdoc cref=""Green"" />
                        class Blue
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("This is a summary.", GetResult(results, "Blue")["Summary"]);
            }

            [Test]
            public async Task InheritDoesNotOverrideExistingSummary()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>This is a summary.</summary>
                        class Green
                        {
                        }

                        /// <inheritdoc />
                        /// <summary>Blue summary.</summary>
                        class Blue : Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Blue summary.", GetResult(results, "Blue")["Summary"]);
            }

            [Test]
            public async Task InheritFromOverriddenMethod()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Green summary.</summary>
                        class Green
                        {
                            /// <summary>Base summary.</summary>
                            public virtual void Foo() {}
                        }

                        /// <summary>Blue summary.</summary>
                        class Blue : Green
                        {
                            /// <inheritdoc />
                            public override void Foo() {}
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Base summary.", GetMember(results, "Blue", "Foo")["Summary"]);
            }

            [Test]
            public async Task InheritFromOverriddenMethodWithParams()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Green summary.</summary>
                        class Green
                        {
                            /// <param name=""a"">AAA</param>
                            /// <param name=""b"">BBB</param>
                            public virtual void Foo(string a, string b) {}
                        }

                        /// <summary>Blue summary.</summary>
                        class Blue : Green
                        {
                            /// <inheritdoc />
                            /// <param name=""b"">XXX</param>
                            public override void Foo(string a, string b) {}
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual(
                    "b",
                    GetMember(results, "Blue", "Foo").List<ReferenceComment>("Params")[0].Name);
                Assert.AreEqual(
                    "XXX",
                    GetMember(results, "Blue", "Foo").List<ReferenceComment>("Params")[0].Html);
                Assert.AreEqual(
                    "a",
                    GetMember(results, "Blue", "Foo").List<ReferenceComment>("Params")[1].Name);
                Assert.AreEqual(
                    "AAA",
                    GetMember(results, "Blue", "Foo").List<ReferenceComment>("Params")[1].Html);
            }

            [Test]
            public async Task InheritFromInterface()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Green summary.</summary>
                        interface IGreen
                        {
                        }

                        /// <inheritdoc />
                        class Blue : IGreen
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Green summary.", GetResult(results, "Blue")["Summary"]);
            }

            [Test]
            public async Task InheritFromMultipleInterfaces()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Red summary.</summary>
                        interface IRed
                        {
                        }

                        interface IGreen
                        {
                        }

                        /// <inheritdoc />
                        class Blue : IGreen, IRed
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Red summary.", GetResult(results, "Blue")["Summary"]);
            }

            [Test]
            public async Task InheritFromMultipleInterfacesWithMultipleMatches()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Red summary.</summary>
                        interface IRed
                        {
                        }

                        /// <summary>Green summary.</summary>
                        interface IGreen
                        {
                        }

                        /// <inheritdoc />
                        class Blue : IGreen, IRed
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Green summary.", GetResult(results, "Blue")["Summary"]);
            }

            [Test]
            public async Task InheritFromImplementedMethod()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>Green summary.</summary>
                        interface IGreen
                        {
                            /// <summary>Interface summary.</summary>
                            void Foo();
                        }

                        /// <summary>Blue summary.</summary>
                        class Blue : IGreen
                        {
                            /// <inheritdoc />
                            public void Foo() {}
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Interface summary.", GetMember(results, "Blue", "Foo")["Summary"]);
            }

            [Test]
            public async Task InheritFromImplementedMethodIfOverride()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public interface IGreen
                        {
                            /// <summary>Interface summary.</summary>
                            void Foo();
                        }

                        public abstract class Red : IGreen
                        {
                            public abstract void Foo();
                        }

                        public class Blue : Red
                        {
                            /// <inheritdoc />
                            public override void Foo() {}
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Interface summary.", GetMember(results, "Blue", "Foo")["Summary"]);
            }

            [Test]
            public async Task InheritFromBaseMethodIfOverrideAndInterface()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public interface IGreen
                        {
                            /// <summary>Interface summary.</summary>
                            void Foo();
                        }

                        public abstract class Red : IGreen
                        {
                            /// <summary>Base summary.</summary>
                            public abstract void Foo();
                        }

                        public class Blue : Red
                        {
                            /// <inheritdoc />
                            public override void Foo() {}
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Base summary.", GetMember(results, "Blue", "Foo")["Summary"]);
            }

            [Test]
            public async Task InheritFromImplementedMethodIfIndirectOverride()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public interface IGreen
                        {
                            /// <summary>Interface summary.</summary>
                            void Foo();
                        }

                        public abstract class Yellow : IGreen
                        {
                            public abstract void Foo();
                        }

                        public abstract class Red : Yellow
                        {
                        }

                        public class Blue : Red
                        {
                            /// <inheritdoc />
                            public override void Foo() {}
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Interface summary.", GetMember(results, "Blue", "Foo")["Summary"]);
            }

            [Test]
            public async Task SummaryWithCdata()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <summary>
                        /// <![CDATA[
                        /// <foo>bar</foo>
                        /// ]]>
                        /// </summary>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    &lt;foo&gt;bar&lt;/foo&gt;\n    ", GetResult(results, "Green")["Summary"]);
            }

            [Test]
            public async Task ExampleCodeWithCdata()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        /// <example>
                        /// <code>
                        /// <![CDATA[
                        /// <foo>bar</foo>
                        /// ]]>
                        /// </code>
                        /// </example>
                        class Green
                        {
                        }
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("\n    <pre><code>&lt;foo&gt;bar&lt;/foo&gt;</code></pre>\n    ", GetResult(results, "Green")["Example"]);
            }
        }
    }
}