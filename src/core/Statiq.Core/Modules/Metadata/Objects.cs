using System.Collections.Generic;
using System.Threading.Tasks;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.Modules;

namespace Statiq.Core.Modules.Metadata
{
    /// <summary>
    /// Creates documents from a set of objects. Any input documents will be ignored.
    /// The objects can be anything, and the collection can be heterogenous.
    /// Dictionary&lt;string, object&gt; will be handled natively and each key-value pair
    /// will be added to the output document metdata.
    /// Anything else will be reflected and turned into a Dictionary&lt;string, object&gt;
    /// with metadata for each property.
    /// </summary>
    /// <category>Metadata</category>
    public class Objects : ReadDataModule<Objects, object>
    {
        private readonly IEnumerable<object> _objects;

        /// <summary>
        /// Creates documents from the specified objects.
        /// </summary>
        /// <param name="objects">The objects to create documents for.</param>
        public Objects(IEnumerable<object> objects)
        {
            _objects = objects;
        }

        /// <inheritdoc />
        protected override Task<IEnumerable<object>> GetItemsAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context) => Task.FromResult(_objects);
    }
}