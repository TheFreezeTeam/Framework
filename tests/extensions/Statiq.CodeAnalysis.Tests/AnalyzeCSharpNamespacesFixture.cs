﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.IO;
using Statiq.Common.Meta;
using Statiq.Common.Modules;
using Statiq.Testing.Documents;
using Statiq.Testing.Execution;

namespace Statiq.CodeAnalysis.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class AnalyzeCSharpNamespacesFixture : AnalyzeCSharpBaseFixture
    {
        public class ExecuteTests : AnalyzeCSharpNamespacesFixture
        {
            [Test]
            public async Task GetsTopLevelNamespaces()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                    }

                    namespace Bar
                    {
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                CollectionAssert.AreEquivalent(new[] { string.Empty, "Foo", "Bar" }, results.Select(x => x["Name"]));
            }

            [Test]
            public async Task TopLevelNamespaceContainsDirectlyNestedNamespaces()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                    }

                    namespace Foo.Baz
                    {
                    }

                    namespace Bar
                    {
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                CollectionAssert.AreEquivalent(new[] { string.Empty, "Foo", "Baz", "Bar" }, results.Select(x => x["Name"]));
                CollectionAssert.AreEquivalent(
                    new[] { "Foo", "Bar" },
                    results.Single(x => x["Name"].Equals(string.Empty)).Get<IEnumerable<IDocument>>("MemberNamespaces").Select(x => x["Name"]));
            }

            [Test]
            public async Task NestedNamespaceContainsDirectlyNestedNamespaces()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                    }

                    namespace Foo.Baz
                    {
                    }

                    namespace Foo.Bar
                    {
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                CollectionAssert.AreEquivalent(new[] { string.Empty, "Foo", "Baz", "Bar" }, results.Select(x => x["Name"]));
                CollectionAssert.AreEquivalent(
                    new[] { "Baz", "Bar" },
                    results.Single(x => x["Name"].Equals("Foo")).Get<IEnumerable<IDocument>>("MemberNamespaces").Select(x => x["Name"]));
            }

            [Test]
            public async Task FullNameDoesNotContainFullHierarchy()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                    }

                    namespace Foo.Bar
                    {
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                CollectionAssert.AreEquivalent(new[] { string.Empty, "Foo", "Bar" }, results.Select(x => x["FullName"]));
            }

            [Test]
            public async Task QualifiedNameContainsFullHierarchy()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                    }

                    namespace Foo.Bar
                    {
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                CollectionAssert.AreEquivalent(new[] { string.Empty, "Foo", "Foo.Bar" }, results.Select(x => x["QualifiedName"]));
            }

            [Test]
            public async Task DisplayNameContainsFullHierarchy()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                    }

                    namespace Foo.Bar
                    {
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                CollectionAssert.AreEquivalent(new[] { "global", "Foo", "Foo.Bar" }, results.Select(x => x["DisplayName"]));
            }

            [Test]
            public async Task NamespaceKindIsNamespace()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                    }

                    namespace Foo.Bar
                    {
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                CollectionAssert.AreEquivalent(new[] { "Namespace", "Namespace", "Namespace" }, results.Select(x => x["Kind"]));
            }

            [Test]
            public async Task NestedNamespacesReferenceParents()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                    }

                    namespace Foo.Bar
                    {
                    }
                ";
                TestDocument document = GetDocument(code);
                TestExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                IReadOnlyList<TestDocument> results = await ExecuteAsync(document, context, module);

                // Then
                Assert.AreEqual("Foo", results.Single(x => x["Name"].Equals("Bar")).Get<IDocument>("ContainingNamespace")["Name"]);
                Assert.AreEqual(string.Empty, results.Single(x => x["Name"].Equals("Foo")).Get<IDocument>("ContainingNamespace")["Name"]);
            }

            [Test]
            public async Task NamespacesContainTypes()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Red
                        {
                        }
                    }

                    namespace Foo.Bar
                    {
                        class Blue
                        {
                        }

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
                CollectionAssert.AreEquivalent(
                    new[] { "Red" },
                    results.Single(x => x["Name"].Equals("Foo")).Get<IEnumerable<IDocument>>("MemberTypes").Select(x => x["Name"]));
                CollectionAssert.AreEquivalent(
                    new[] { "Blue", "Green" },
                    results.Single(x => x["Name"].Equals("Bar")).Get<IEnumerable<IDocument>>("MemberTypes").Select(x => x["Name"]));
            }

            [Test]
            public async Task NamespacesDoNotContainNestedTypes()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Blue
                        {
                            class Green
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
                CollectionAssert.AreEquivalent(
                    new[] { "Blue" },
                    results.Single(x => x["Name"].Equals("Foo")).Get<IEnumerable<IDocument>>("MemberTypes").Select(x => x["Name"]));
            }

            [Test]
            public async Task DestinationPathIsCorrect()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        namespace Bar
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
                CollectionAssert.AreEquivalent(
                    new[] { "global/index.html", "Foo/index.html", "Foo.Bar/index.html" },
                    results.Where(x => x["Kind"].Equals("Namespace")).Select(x => x.Destination.FullPath));
            }
        }
    }
}
