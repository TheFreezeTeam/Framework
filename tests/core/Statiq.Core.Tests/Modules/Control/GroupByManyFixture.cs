﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Statiq.Common;
using Statiq.Common.Configuration;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.Meta;
using Statiq.Core.Modules.Control;
using Statiq.Core.Modules.Extensibility;
using Statiq.Testing;
using Statiq.Testing.Modules;

namespace Statiq.Core.Tests.Modules.Control
{
    [TestFixture]
    public class GroupByManyFixture : BaseFixture
    {
        public class ExecuteTests : GroupByManyFixture
        {
            [Test]
            public async Task SetsCorrectMetadata()
            {
                // Given
                List<int> groupKey = new List<int>();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7,
                    EnsureInputDocument = true
                };
                GroupByMany groupByMany = new GroupByMany(Config.FromDocument(d => new[] { d.Get<int>("A") % 3, 3 }), count);
                Execute gatherData = new ExecuteDocument(
                    Config.FromDocument(d =>
                    {
                        groupKey.Add(d.Get<int>(Keys.GroupKey));
                        return d;
                    }), false);

                // When
                IReadOnlyList<IDocument> results = await ExecuteAsync(groupByMany, gatherData);

                // Then
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, groupKey);
            }

            [Test]
            public async Task SetsDocumentsInMetadata()
            {
                // Given
                List<IList<string>> content = new List<IList<string>>();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7,
                    EnsureInputDocument = true
                };
                GroupByMany groupByMany = new GroupByMany(Config.FromDocument(d => new[] { d.Get<int>("A") % 3, 3 }), count);
                OrderBy orderBy = new OrderBy(Config.FromDocument(d => d.Get<int>(Keys.GroupKey)));
                Execute gatherData = new ExecuteDocument(
                    Config.FromDocument(async d =>
                    {
                        IEnumerable<string> groupContent = await d.Get<IList<IDocument>>(Keys.GroupDocuments).SelectAsync(async x => await x.GetStringAsync());
                        content.Add(groupContent.ToList());
                        return (object)null;
                    }), false);

                // When
                IReadOnlyList<IDocument> results = await ExecuteAsync(groupByMany, orderBy, gatherData);

                // Then
                Assert.AreEqual(4, content.Count);
                CollectionAssert.AreEquivalent(new[] { "3", "6" }, content[0]);
                CollectionAssert.AreEquivalent(new[] { "1", "4", "7" }, content[1]);
                CollectionAssert.AreEquivalent(new[] { "2", "5", "8" }, content[2]);
                CollectionAssert.AreEquivalent(new[] { "1", "2", "3", "4", "5", "6", "7", "8" }, content[3]);
            }

            [Test]
            public async Task GroupByMetadataKey()
            {
                // Given
                List<int> groupKey = new List<int>();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7,
                    EnsureInputDocument = true
                };
                Core.Modules.Metadata.Meta meta = new Core.Modules.Metadata.Meta("GroupMetadata", Config.FromDocument(d => new object[] { d.Get<int>("A") % 3, 3 }));
                GroupByMany groupByMany = new GroupByMany("GroupMetadata", count, meta);
                Execute gatherData = new ExecuteDocument(
                    Config.FromDocument(d =>
                    {
                        groupKey.Add(d.Get<int>(Keys.GroupKey));
                        return (object)null;
                    }), false);

                // When
                IReadOnlyList<IDocument> results = await ExecuteAsync(groupByMany, gatherData);

                // Then
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, groupKey);
            }

            [Test]
            public async Task GroupByMetadataKeyWithMissingMetadata()
            {
                // Given
                List<int> groupKey = new List<int>();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7,
                    EnsureInputDocument = true
                };
                Execute meta = new ExecuteDocument(
                    Config.FromDocument(d =>
                    {
                        int groupMetadata = d.Get<int>("A") % 3;
                        return groupMetadata == 0 ? d : d.Clone(new MetadataItems { { "GroupMetadata", new object[] { groupMetadata, 3 } } });
                    }), false);
                GroupByMany groupByMany = new GroupByMany("GroupMetadata", count, meta);
                Execute gatherData = new ExecuteDocument(
                    Config.FromDocument(d =>
                    {
                        groupKey.Add(d.Get<int>(Keys.GroupKey));
                        return (object)null;
                    }), false);

                // When
                IReadOnlyList<IDocument> results = await ExecuteAsync(groupByMany, gatherData);

                // Then
                CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, groupKey);
            }

            [Test]
            public async Task DefaultComparerIsCaseSensitive()
            {
                // Given
                List<object> groupKey = new List<object>();
                Execute meta = new ExecuteContext(
                    c => new IDocument[]
                    {
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { "A", "b" } } }),
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { "B" } } }),
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { "C" } } }),
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { "c" } } }),
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { 1 } } }),
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { "1" } } })
                    });
                GroupByMany groupByMany = new GroupByMany("Tag", meta);
                Execute gatherData = new ExecuteDocument(
                    Config.FromDocument(d =>
                    {
                        groupKey.Add(d.Get(Keys.GroupKey));
                        return (object)null;
                    }), false);

                // When
                IReadOnlyList<IDocument> results = await ExecuteAsync(groupByMany, gatherData);

                // Then
                CollectionAssert.AreEquivalent(new object[] { "A", "B", "b", "C", "c", 1, "1" }, groupKey);
            }

            [Test]
            public async Task CaseInsensitiveStringComparer()
            {
                // Given
                List<object> groupKey = new List<object>();
                Execute meta = new ExecuteContext(
                    c => new IDocument[]
                    {
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { "A", "b" } } }),
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { "B" } } }),
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { "C" } } }),
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { "c" } } }),
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { 1 } } }),
                        c.GetDocument(new MetadataItems { { "Tag", new object[] { "1" } } })
                    });
                GroupByMany groupByMany = new GroupByMany("Tag", meta).WithComparer(StringComparer.OrdinalIgnoreCase);
                Execute gatherData = new ExecuteDocument(
                    Config.FromDocument(d =>
                    {
                        groupKey.Add(d.Get(Keys.GroupKey));
                        return (object)null;
                    }), false);

                // When
                IReadOnlyList<IDocument> results = await ExecuteAsync(groupByMany, gatherData);

                // Then
                CollectionAssert.AreEquivalent(new object[] { "A", "b", "C", 1 }, groupKey);
            }

            [Test]
            public async Task ExcludesDocumentsThatDontMatchPredicate()
            {
                // Given
                List<int> groupKey = new List<int>();
                CountModule count = new CountModule("A")
                {
                    AdditionalOutputs = 7,
                    EnsureInputDocument = true
                };
                GroupByMany groupByMany = new GroupByMany(Config.FromDocument(d => new[] { d.Get<int>("A") % 3, 3 }), count)
                    .Where(Config.FromDocument(d => d.Get<int>("A") % 3 != 0));
                Execute gatherData = new ExecuteDocument(
                    Config.FromDocument(d =>
                    {
                        groupKey.Add(d.Get<int>(Keys.GroupKey));
                        return (object)null;
                    }), false);

                // When
                IReadOnlyList<IDocument> results = await ExecuteAsync(groupByMany, gatherData);

                // Then
                CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, groupKey);
            }
        }
    }
}
