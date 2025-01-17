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
    public class TableFixture : BaseFixture
    {
        public class ExecuteTests : TableFixture
        {
            [Test]
            public async Task RendersTableWithoutSettings()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                TestDocument document = new TestDocument();
                string content = @"
1 2 ""3 4""
a ""b c"" d
e f g
5 678
""h i""  j ""k""
l=m nop
";
                KeyValuePair<string, string>[] args = new KeyValuePair<string, string>[] { };
                Table shortcode = new Table();

                // When
                TestDocument result = (TestDocument)await shortcode.ExecuteAsync(args, content, document, context);

                // Then
                result.Content.ShouldBe(
                    @"<table>
  <tbody>
    <tr>
      <td>1</td>
      <td>2</td>
      <td>3 4</td>
    </tr>
    <tr>
      <td>a</td>
      <td>b c</td>
      <td>d</td>
    </tr>
    <tr>
      <td>e</td>
      <td>f</td>
      <td>g</td>
    </tr>
    <tr>
      <td>5</td>
      <td>678</td>
    </tr>
    <tr>
      <td>h i</td>
      <td>j</td>
      <td>k</td>
    </tr>
    <tr>
      <td>l=m</td>
      <td>nop</td>
    </tr>
  </tbody>
</table>",
                    StringCompareShould.IgnoreLineEndings);
            }

            [Test]
            public async Task RendersTableWithSettings()
            {
                // Given
                TestExecutionContext context = new TestExecutionContext();
                TestDocument document = new TestDocument();
                string content = @"
1 2 ""3 4""
a ""b c"" d
e f g
5 678
""h i""  j ""k""
l=m nop
";
                KeyValuePair<string, string>[] args = new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>("Class", "tclass"),
                    new KeyValuePair<string, string>("HeaderRows", "1"),
                    new KeyValuePair<string, string>("FooterRows", "2"),
                    new KeyValuePair<string, string>("HeaderCols", "1"),
                    new KeyValuePair<string, string>("HeaderClass", "hclass"),
                    new KeyValuePair<string, string>("BodyClass", "bclass"),
                    new KeyValuePair<string, string>("FooterClass", "fclass")
                };
                Table shortcode = new Table();

                // When
                TestDocument result = (TestDocument)await shortcode.ExecuteAsync(args, content, document, context);

                // Then
                result.Content.ShouldBe(
                    @"<table class=""tclass"">
  <thead class=""hclass"">
    <tr>
      <th>1</th>
      <th>2</th>
      <th>3 4</th>
    </tr>
  </thead>
  <tbody class=""bclass"">
    <tr>
      <th>a</th>
      <td>b c</td>
      <td>d</td>
    </tr>
    <tr>
      <th>e</th>
      <td>f</td>
      <td>g</td>
    </tr>
    <tr>
      <th>5</th>
      <td>678</td>
    </tr>
  </tbody>
  <tfoot class=""fclass"">
    <tr>
      <th>h i</th>
      <td>j</td>
      <td>k</td>
    </tr>
    <tr>
      <th>l=m</th>
      <td>nop</td>
    </tr>
  </tfoot>
</table>",
                    StringCompareShould.IgnoreLineEndings);
            }
        }
    }
}
