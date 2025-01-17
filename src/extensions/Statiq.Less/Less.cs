﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using dotless.Core;
using dotless.Core.configuration;
using ReflectionMagic;
using Statiq.Common.Configuration;
using Statiq.Common.Documents;
using Statiq.Common.Meta;
using Statiq.Common.Modules;
using Statiq.Common.Execution;
using Statiq.Common.IO;
using Statiq.Common.Tracing;

namespace Statiq.Less
{
    /// <summary>
    /// Compiles Less CSS files to CSS stylesheets.
    /// </summary>
    /// <remarks>
    /// The content of the input document is compiled to CSS and the content of the output document contains the compiled CSS stylesheet.
    /// </remarks>
    /// <example>
    /// This is a pipeline that compiles two Less CSS files, one for Bootstrap (which contains a lot of includes) and a second for custom CSS.
    /// <code>
    /// Pipelines.Add("Less",
    ///     ReadFiles("master.less"),
    ///     Concat(ReadFiles("bootstrap.less")),
    ///     Less(),
    ///     WriteFiles(".css")
    /// );
    /// </code>
    /// </example>
    /// <category>Templates</category>
    public class Less : IModule
    {
        private DocumentConfig<FilePath> _inputPath = Config.FromDocument(doc => doc.Source);

        /// <summary>
        /// Specifies a delegate that should be used to get the input path for each
        /// input document. This allows the Sass processor to search the right
        /// file system and paths for include files. By default, the value of
        /// <see cref="IDocument.Source"/> is used for the input document path.
        /// </summary>
        /// <param name="inputPath">A delegate that should return a <see cref="FilePath"/>.</param>
        /// <returns>The current instance.</returns>
        public Less WithInputPath(DocumentConfig<FilePath> inputPath)
        {
            _inputPath = inputPath ?? throw new ArgumentNullException(nameof(inputPath));
            return this;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            DotlessConfiguration config = DotlessConfiguration.GetDefault();
            config.Logger = typeof(LessLogger);
            EngineFactory engineFactory = new EngineFactory(config);
            FileSystemReader fileSystemReader = new FileSystemReader(context.FileSystem);
            return await inputs.ParallelSelectAsync(context, ProcessLessAsync);

            async Task<IDocument> ProcessLessAsync(IDocument input)
            {
                Trace.Verbose("Processing Less for {0}", input.Source.ToDisplayString());
                ILessEngine engine = engineFactory.GetEngine();

                // TODO: Get rid of RefelectionMagic and this ugly hack as soon as dotless gets better external DI support
                engine.AsDynamic().Underlying.Underlying.Parser.Importer.FileReader = fileSystemReader;

                // Less conversion
                FilePath path = await _inputPath.GetValueAsync(input, context);
                if (path != null)
                {
                    engine.CurrentDirectory = path.Directory.FullPath;
                }
                else
                {
                    engine.CurrentDirectory = string.Empty;
                    path = new FilePath(Path.GetRandomFileName());
                    Trace.Warning($"No input path found for document {input.Source.ToDisplayString()}, using {path.FileName.FullPath}");
                }
                string content = engine.TransformToCss(await input.GetStringAsync(), path.FileName.FullPath);

                // Process the result
                FilePath cssPath = path.GetRelativeInputPath(context).ChangeExtension("css");
                return input.Clone(
                    cssPath,
                    await context.GetContentProviderAsync(content));
            }
        }
    }
}
