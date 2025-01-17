﻿using Statiq.Common.Configuration;
using Statiq.Common.Documents;
using Statiq.Common.IO;
using Statiq.Common.Shortcodes;

namespace Statiq.Common.Execution
{
    /// <summary>
    /// The engine is the primary entry point for the generation process.
    /// </summary>
    public interface IEngine : IConfigurable, IDocumentFactoryProvider
    {
        /// <summary>
        /// Gets the file system.
        /// </summary>
        IFileSystem FileSystem { get; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        ISettings Settings { get; }

        /// <summary>
        /// Gets the pipelines.
        /// </summary>
        IPipelineCollection Pipelines { get; }

        /// <summary>
        /// Gets the shortcodes.
        /// </summary>
        IShortcodeCollection Shortcodes { get; }

        /// <summary>
        /// Gets the namespaces that should be brought in scope by modules that support dynamic compilation.
        /// </summary>
        INamespacesCollection Namespaces { get; }

        /// <summary>
        /// Gets a collection of all the raw assemblies that should be referenced by modules
        /// that support dynamic compilation (such as configuration assemblies).
        /// </summary>
        IRawAssemblyCollection DynamicAssemblies { get; }

        /// <summary>
        /// Provides pooled memory streams (via the RecyclableMemoryStream library).
        /// </summary>
        IMemoryStreamFactory MemoryStreamFactory { get; }

        /// <summary>
        /// Gets or sets the application input.
        /// </summary>
        string ApplicationInput { get; set; }
    }
}
