﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Statiq.Core.Shortcodes.Html;
using Statiq.Testing;
using Statiq.Testing.Documents;
using Statiq.Testing.Execution;

namespace Statiq.Core.Tests.Shortcodes.Html
{
    [TestFixture]
    public class FigureFixture : BaseFixture
    {
        public class ExecuteTests : FigureFixture
        {
            [Test]
            public async Task RendersFigure()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                TestDocument document = new TestDocument();
                KeyValuePair<string, string>[] args = new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>(null, "/a/b"),
                    new KeyValuePair<string, string>(null, "/c/d"),
                    new KeyValuePair<string, string>(null, "abc"),
                    new KeyValuePair<string, string>(null, "def"),
                    new KeyValuePair<string, string>(null, "ghi"),
                    new KeyValuePair<string, string>(null, "jkl"),
                    new KeyValuePair<string, string>(null, "100px"),
                    new KeyValuePair<string, string>(null, "200px")
                };
                Figure shortcode = new Figure();

                // When
                TestDocument result = (TestDocument)await shortcode.ExecuteAsync(args, "foo bar", document, context);

                // Then
                result.Content.ShouldBe(
                    @"<figure class=""jkl"">
  <a href=""/c/d"" target=""abc"" rel=""def"">
    <img src=""/a/b"" alt=""ghi"" height=""100px"" width=""200px"" />
  </a>
  <figcaption>foo bar</figcaption>
</figure>",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task RendersFigureWithoutLink()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                TestDocument document = new TestDocument();
                KeyValuePair<string, string>[] args = new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>("Src", "/a/b"),
                    new KeyValuePair<string, string>("Alt", "ghi"),
                    new KeyValuePair<string, string>("Class", "jkl"),
                    new KeyValuePair<string, string>("Height", "100px"),
                    new KeyValuePair<string, string>("Width", "200px")
                };
                Figure shortcode = new Figure();

                // When
                TestDocument result = (TestDocument)await shortcode.ExecuteAsync(args, "foo bar", document, context);

                // Then
                result.Content.ShouldBe(
                    @"<figure class=""jkl"">
  <img src=""/a/b"" alt=""ghi"" height=""100px"" width=""200px"" />
  <figcaption>foo bar</figcaption>
</figure>",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task DoesNotRenderLinkIfNoImage()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                TestDocument document = new TestDocument();
                KeyValuePair<string, string>[] args = new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>("Link", "/c/d"),
                    new KeyValuePair<string, string>("Target", "abc"),
                    new KeyValuePair<string, string>("Rel", "def"),
                    new KeyValuePair<string, string>("Alt", "ghi"),
                    new KeyValuePair<string, string>("Class", "jkl"),
                    new KeyValuePair<string, string>("Height", "100px"),
                    new KeyValuePair<string, string>("Width", "200px")
                };
                Figure shortcode = new Figure();

                // When
                TestDocument result = (TestDocument)await shortcode.ExecuteAsync(args, "foo bar", document, context);

                // Then
                result.Content.ShouldBe(
                    @"<figure class=""jkl"">
  <figcaption>foo bar</figcaption>
</figure>",
                    StringCompareShould.IgnoreLineEndings);
            }
        }
    }
}
