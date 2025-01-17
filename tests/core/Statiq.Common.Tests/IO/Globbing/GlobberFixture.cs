﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Statiq.Common.IO;
using Statiq.Common.IO.Globbing;
using Statiq.Testing;
using Statiq.Testing.Attributes;
using Statiq.Testing.IO;

namespace Statiq.Common.Tests.IO.Globbing
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class GlobberFixture : BaseFixture
    {
        public class GetFilesTests : GlobberFixture
        {
            [TestCase("/a", new[] { "b/c/foo.txt" }, new[] { "/a/b/c/foo.txt" })]
            [TestCase("/a", new[] { "**/foo.txt" }, new[] { "/a/b/c/foo.txt" })]
            [TestCase("/a", new[] { "**/baz.txt" }, new[] { "/a/b/c/baz.txt", "/a/b/d/baz.txt" })]
            [TestCase("/a/b/d", new[] { "**/baz.txt" }, new[] { "/a/b/d/baz.txt" })]
            [TestCase("/a", new[] { "**/c/baz.txt" }, new[] { "/a/b/c/baz.txt" })]
            [TestCase("/a", new[] { "**/c/**/baz.txt" }, new[] { "/a/b/c/baz.txt" })]
            [TestCase("/a", new[] { "**/c/*/baz.txt" }, new string[] { })]
            [TestCase("/a", new[] { "**/foo.txt", "**/baz.txt" }, new[] { "/a/b/c/foo.txt", "/a/b/c/baz.txt", "/a/b/d/baz.txt" })]
            [TestCase("/a", new[] { "**/foo.txt", "**/c/baz.txt" }, new[] { "/a/b/c/foo.txt", "/a/b/c/baz.txt" })]
            [TestCase("/a", new[] { "**/baz.txt", "!**/d/*" }, new[] { "/a/b/c/baz.txt" })]
            [TestCase("/a", new[] { "**/*.txt" }, new[] { "/a/b/c/foo.txt", "/a/b/c/baz.txt", "/a/b/c/1/2.txt", "/a/b/d/baz.txt", "/a/x/bar.txt", "/a/foo/bar/a.txt", "/a/foo/baz/b.txt" })]
            [TestCase("/a/b/c", new[] { "*.txt" }, new[] { "/a/b/c/foo.txt", "/a/b/c/baz.txt" })]
            [TestCase("/a", new[] { "**/*.txt", "!**/b*.txt" }, new[] { "/a/b/c/foo.txt", "/a/b/c/1/2.txt", "/a/foo/bar/a.txt" })]
            [TestCase("/a", new[] { "x/*.{xml,txt}" }, new[] { "/a/x/bar.txt", "/a/x/foo.xml" })]
            [TestCase("/a", new[] { "x/*.\\{xml,txt\\}" }, new string[] { })]
            [TestCase("/a", new[] { "x/*", "!x/*.{xml,txt}" }, new[] { "/a/x/foo.doc" })]
            [TestCase("/a", new[] { "x/*", "\\!x/*.{xml,txt}" }, new[] { "/a/x/bar.txt", "/a/x/foo.xml", "/a/x/foo.doc" })] // TODO: Only change slashes if they don't precede and escape character
            [TestCase("/a", new[] { "x/*.{*,!doc}" }, new[] { "/a/x/bar.txt", "/a/x/foo.xml" })]
            [TestCase("/a", new[] { "x/*{!doc,}" }, new[] { "/a/x/bar.txt", "/a/x/foo.xml" })]
            [TestCase("/a/b/c", new[] { "../d/*.txt" }, new[] { "/a/b/d/baz.txt" })]
            [TestCase("/a", new[] { "foo/*/*.txt" }, new[] { "/a/foo/bar/a.txt", "/a/foo/baz/b.txt" })]
            [TestCase("/a", new[] { "foo/*{!r,}/*.txt" }, new[] { "/a/foo/baz/b.txt" })]
            [TestCase("/a/x", new[] { "../b/**/1/*.txt" }, new[] { "/a/b/c/1/2.txt" })]
            [TestCase("/a/b", new[] { "**/1/*.txt" }, new[] { "/a/b/c/1/2.txt" })]
            [TestCase("/a", new[] { "x/*.{txt,xml,doc}" }, new[] { "/a/x/bar.txt", "/a/x/foo.xml", "/a/x/foo.doc" })]
            public async Task ShouldReturnMatchedFiles(string directoryPath, string[] patterns, string[] resultPaths)
            {
                // Given
                IFileProvider fileProvider = GetFileProvider();
                IDirectory directory = await fileProvider.GetDirectoryAsync(directoryPath);

                // When
                IEnumerable<IFile> matches = await Globber.GetFilesAsync(directory, patterns);
                IEnumerable<IFile> matchesReversedSlash = await Globber.GetFilesAsync(directory, patterns.Select(x => x.Replace("/", "\\")));

                // Then
                CollectionAssert.AreEquivalent(resultPaths, matches.Select(x => x.Path.FullPath));
                CollectionAssert.AreEquivalent(resultPaths, matchesReversedSlash.Select(x => x.Path.FullPath));
            }

            [Test]
            public async Task DoubleWildcardShouldMatchZeroOrMorePathSegments()
            {
                // Given
                TestFileProvider fileProvider = new TestFileProvider();
                fileProvider.AddDirectory("/");
                fileProvider.AddDirectory("/root");
                fileProvider.AddDirectory("/root/a");
                fileProvider.AddDirectory("/root/a/b");
                fileProvider.AddDirectory("/root/d");
                fileProvider.AddFile("/root/a/x.txt");
                fileProvider.AddFile("/root/a/b/x.txt");
                fileProvider.AddFile("/root/d/x.txt");
                IDirectory directory = await fileProvider.GetDirectoryAsync("/");

                // When
                IEnumerable<IFile> matches = await Globber.GetFilesAsync(directory, new[] { "root/{a,}/**/x.txt" });

                // Then
                matches.Select(x => x.Path.FullPath).ShouldBe(
                    new[] { "/root/a/x.txt", "/root/a/b/x.txt", "/root/d/x.txt" }, true);
            }

            [Test]
            public async Task WildcardShouldMatchZeroOrMore()
            {
                // Given
                TestFileProvider fileProvider = new TestFileProvider();
                fileProvider.AddDirectory("/");
                fileProvider.AddDirectory("/root");
                fileProvider.AddDirectory("/root/a");
                fileProvider.AddDirectory("/root/a/b");
                fileProvider.AddDirectory("/root/d");
                fileProvider.AddFile("/root/a/x.txt");
                fileProvider.AddFile("/root/a/b/x.txt");
                fileProvider.AddFile("/root/a/b/.txt");
                fileProvider.AddFile("/root/d/x.txt");
                IDirectory directory = await fileProvider.GetDirectoryAsync("/");

                // When
                IEnumerable<IFile> matches = await Globber.GetFilesAsync(directory, new[] { "root/**/*.txt" });

                // Then
                matches.Select(x => x.Path.FullPath).ShouldBe(
                    new[] { "/root/a/x.txt", "/root/a/b/x.txt", "/root/a/b/.txt", "/root/d/x.txt" }, true);
            }

            [TestCase("/a/x", new[] { "../b/c/**/1/2/*.txt" }, new[] { "/a/b/c/d/e/1/2/3.txt" })]
            [TestCase("/a/x", new[] { "../b/c/d/e/1/2/*.txt" }, new[] { "/a/b/c/d/e/1/2/3.txt" })]
            public async Task RecursiveWildcardTests(string directoryPath, string[] patterns, string[] resultPaths)
            {
                // Given
                TestFileProvider fileProvider = new TestFileProvider();
                fileProvider.AddDirectory("/a");
                fileProvider.AddDirectory("/a/b");
                fileProvider.AddDirectory("/a/b/c");
                fileProvider.AddDirectory("/a/b/c/d");
                fileProvider.AddDirectory("/a/b/c/d/e");
                fileProvider.AddDirectory("/a/b/c/d/e/1");
                fileProvider.AddDirectory("/a/b/c/d/e/1/2");
                fileProvider.AddFile("/a/b/c/d/e/1/2/3.txt");
                IDirectory directory = await fileProvider.GetDirectoryAsync(directoryPath);

                // When
                IEnumerable<IFile> matches = await Globber.GetFilesAsync(directory, patterns);
                IEnumerable<IFile> matchesReversedSlash = await Globber.GetFilesAsync(directory, patterns.Select(x => x.Replace("/", "\\")));

                // Then
                CollectionAssert.AreEquivalent(resultPaths, matches.Select(x => x.Path.FullPath));
                CollectionAssert.AreEquivalent(resultPaths, matchesReversedSlash.Select(x => x.Path.FullPath));
            }

            // Addresses a specific problem with nested folders in a wildcard search
            // due to an incorrect TestDirectory.GetDirectories() implementation
            // (it was returning non-existing "/a/b/foo/y.txt" as a match)
            [Test]
            public async Task NestedFoldersWilcard()
            {
                // Given
                TestFileProvider fileProvider = new TestFileProvider();
                fileProvider.AddDirectory("/a");
                fileProvider.AddDirectory("/a/b");
                fileProvider.AddDirectory("/a/b/c");
                fileProvider.AddDirectory("/a/bar");
                fileProvider.AddDirectory("/a/bar/foo");
                fileProvider.AddFile("/a/b/c/x.txt");
                fileProvider.AddFile("/a/bar/foo/y.txt");
                IDirectory directory = await fileProvider.GetDirectoryAsync("/a");

                // When
                IEnumerable<IFile> matches = await Globber.GetFilesAsync(directory, new[] { "**/*.txt" });

                // Then
                CollectionAssert.AreEquivalent(new[] { "/a/b/c/x.txt", "/a/bar/foo/y.txt" }, matches.Select(x => x.Path.FullPath));
            }

            [TestCase("/a/b")]
            [TestCase("/**/a")]
            [WindowsTestCase("C:/a")]
            public async Task ShouldThrowForRootPatterns(string pattern)
            {
                // Given
                IFileProvider fileProvider = GetFileProvider();
                IDirectory directory = await fileProvider.GetDirectoryAsync("/a");

                // When, Then
                await Should.ThrowAsync<ArgumentException>(async () => await Globber.GetFilesAsync(directory, new[] { pattern }));
            }
        }

        public class ExpandBracesTests : GlobberFixture
        {
            [TestCase("a/b", new[] { "a/b" })]
            [TestCase("!a/b", new[] { "!a/b" })]
            [TestCase("a/b{x,y}", new[] { "a/bx", "a/by" })]
            [TestCase("a/b{x,!y}", new[] { "a/bx", "!a/by" })]
            [TestCase("a/b{x,\\!y}", new[] { "a/bx", "a/b\\!y" })]
            [TestCase("a/b{x,}", new[] { "a/bx", "a/b" })]
            [TestCase("a/b{!x,}", new[] { "!a/bx", "a/b" })]
            [TestCase("a/b{x,!}", new[] { "a/bx", "!a/b" })]
            [TestCase("a/b.{x,y}", new[] { "a/b.x", "a/b.y" })]
            [TestCase("a/*.{x,y}", new[] { "a/*.x", "a/*.y" })]
            [TestCase("a{b,c}d", new[] { "abd", "acd" })]
            [TestCase("a{b,}c", new[] { "abc", "ac" })]
            [TestCase("a{0..3}d", new[] { "a0d", "a1d", "a2d", "a3d" })]
            [TestCase("a{0..3,5..6}d", new[] { "a0..3d", "a5..6d" })]
            [TestCase("a{b,c{d,e}f}g", new[] { "abg", "acdfg", "acefg" })]
            [TestCase("a{b,c{d,!e}f}g", new[] { "abg", "acdfg", "!acefg" })]
            [TestCase("a{!b,c{d,!e}f}g", new[] { "!abg", "acdfg", "!acefg" })]
            [TestCase("a{b,c}d{e,f}g", new[] { "abdeg", "acdeg", "abdfg", "acdfg" })]
            [TestCase("a{b,!c}d{e,f}g", new[] { "abdeg", "!acdeg", "abdfg", "!acdfg" })]
            [TestCase("a{b,c}d{e,!f}g", new[] { "abdeg", "acdeg", "!abdfg", "!acdfg" })]
            [TestCase("a{b,!c}d{e,!f}g", new[] { "abdeg", "!acdeg", "!abdfg", "!acdfg" })]
            [TestCase("a{2..}b", new[] { "a{2..}b" })]
            [TestCase("a{!2..}b", new[] { "a{!2..}b" })]
            [TestCase("a{b}c", new[] { "a{b}c" })]
            [TestCase("a{b{x,y}}c", new[] { "a{bx}c", "a{by}c" })]
            [TestCase("a{b,c{d,e},{f,g}h}x{y,z}", new[] { "abxy", "abxz", "acdxy", "acdxz", "acexy", "acexz", "afhxy", "afhxz", "aghxy", "aghxz" })]
            [TestCase("{a,b}", new[] { "a", "b" })]
            [TestCase("**/{!_,}*.cshtml", new[] { "**/*.cshtml", "!**/_*.cshtml" })]
            [TestCase("**/{!.foo,}/{!_,}*.cshtml", new[] { "!**/.foo/*.cshtml", "!**/.foo/_*.cshtml", "**/*.cshtml", "!**/_*.cshtml" })]
            public void ShouldExpandBraces(string pattern, string[] expected)
            {
                // Given, When
                IEnumerable<string> result = Globber.ExpandBraces(pattern);

                // Then
                CollectionAssert.AreEquivalent(expected, result);
            }
        }

        private TestFileProvider GetFileProvider()
        {
            TestFileProvider fileProvider = new TestFileProvider();

            fileProvider.AddDirectory("/a");
            fileProvider.AddDirectory("/a/b");
            fileProvider.AddDirectory("/a/b/c");
            fileProvider.AddDirectory("/a/b/c/1");
            fileProvider.AddDirectory("/a/b/d");
            fileProvider.AddDirectory("/a/x");
            fileProvider.AddDirectory("/a/y");
            fileProvider.AddDirectory("/a/y/z");
            fileProvider.AddDirectory("/a/foo");
            fileProvider.AddDirectory("/a/foo/bar");
            fileProvider.AddDirectory("/a/foo/baz");

            fileProvider.AddFile("/a/b/c/foo.txt");
            fileProvider.AddFile("/a/b/c/baz.txt");
            fileProvider.AddFile("/a/b/c/1/2.txt");
            fileProvider.AddFile("/a/b/d/baz.txt");
            fileProvider.AddFile("/a/x/bar.txt");
            fileProvider.AddFile("/a/x/foo.xml");
            fileProvider.AddFile("/a/x/foo.doc");
            fileProvider.AddFile("/a/foo/bar/a.txt");
            fileProvider.AddFile("/a/foo/baz/b.txt");

            return fileProvider;
        }
    }
}
