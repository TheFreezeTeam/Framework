﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Buildalyzer;
using Microsoft.CodeAnalysis;
using Statiq.Common;
using Statiq.Common.Configuration;
using Statiq.Common.Documents;
using Statiq.Common.Execution;
using Statiq.Common.IO;
using Statiq.Common.Meta;
using Statiq.Common.Modules;

namespace Statiq.CodeAnalysis
{
    /// <summary>
    /// Reads an MSBuild solution or project file and returns all referenced source files as documents.
    /// This module will be executed once and input documents will be ignored if a search path is
    /// specified. Otherwise, if a delegate is specified the module will be executed once per input
    /// document and the resulting output documents will be aggregated.
    /// </summary>
    public abstract class ReadWorkspace : IModule
    {
        private readonly DocumentConfig<FilePath> _path;
        private Func<string, bool> _whereProject;
        private Func<IFile, bool> _whereFile;
        private string[] _extensions;

        protected ReadWorkspace(DocumentConfig<FilePath> path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        /// <summary>
        /// Filters the project based on name.
        /// </summary>
        /// <param name="predicate">A predicate that should return <c>true</c> if the project should be included.</param>
        /// <returns>The current module instance.</returns>
        public ReadWorkspace WhereProject(Func<string, bool> predicate)
        {
            Func<string, bool> currentPredicate = _whereProject;
            _whereProject = currentPredicate == null ? predicate : x => currentPredicate(x) && predicate(x);
            return this;
        }

        /// <summary>
        /// Filters the source code file based on path.
        /// </summary>
        /// <param name="predicate">A predicate that should return <c>true</c> if the source code file should be included.</param>
        /// <returns>The current module instance.</returns>
        public ReadWorkspace WhereFile(Func<IFile, bool> predicate)
        {
            Func<IFile, bool> currentPredicate = _whereFile;
            _whereFile = currentPredicate == null ? predicate : x => currentPredicate(x) && predicate(x);
            return this;
        }

        /// <summary>
        /// Filters the source code files based on extension.
        /// </summary>
        /// <param name="extensions">The extensions to include (if defined, any extensions not listed will be excluded).</param>
        /// <returns>The current module instance.</returns>
        public ReadWorkspace WithExtensions(params string[] extensions)
        {
            _extensions = _extensions?.Concat(extensions.Select(x => x.StartsWith(".") ? x : "." + x)).ToArray()
                ?? extensions.Select(x => x.StartsWith(".") ? x : "." + x).ToArray();
            return this;
        }

        /// <summary>
        /// Gets the projects in the workspace (solution or project).
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="file">The project file.</param>
        /// <returns>A sequence of Roslyn <see cref="Project"/> instances in the workspace.</returns>
        protected abstract IEnumerable<Project> GetProjects(IExecutionContext context, IFile file);

        protected internal static AnalyzerResult CompileProjectAndTrace(ProjectAnalyzer analyzer, StringWriter log)
        {
            log.GetStringBuilder().Clear();
            Common.Tracing.Trace.Verbose($"Building project {analyzer.ProjectFile.Path}");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            AnalyzerResult result = analyzer.Build().FirstOrDefault();
            sw.Stop();
            Common.Tracing.Trace.Verbose($"Project {analyzer.ProjectFile.Path} built in {sw.ElapsedMilliseconds} ms");
            if (result?.Succeeded != true)
            {
                Common.Tracing.Trace.Error($"Could not compile project at {analyzer.ProjectFile.Path}");
                Common.Tracing.Trace.Warning(log.ToString());
                return null;
            }
            return result;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IDocument>> ExecuteAsync(IReadOnlyList<IDocument> inputs, IExecutionContext context)
        {
            return await inputs.ParallelSelectManyAsync(async input =>
                await ExecuteAsync(input, await _path.GetValueAsync(input, context), context));
        }

        private Task<IEnumerable<IDocument>> ExecuteAsync(IDocument input, FilePath projectPath, IExecutionContext context)
        {
            return context.TraceExceptionsAsync(input, ExecuteAsync);

            async Task<IEnumerable<IDocument>> ExecuteAsync(IDocument doc)
            {
                if (projectPath != null)
                {
                    IFile projectFile = await context.FileSystem.GetInputFileAsync(projectPath);

                    return await GetProjects(context, projectFile)
                        .Where(project => project != null && (_whereProject == null || _whereProject(project.Name)))
                        .ParallelSelectManyAsync(GetProjectDocumentsAsync);

                    async Task<IEnumerable<IDocument>> GetProjectDocumentsAsync(Project project)
                    {
                        Common.Tracing.Trace.Verbose("Read project {0}", project.Name);
                        string assemblyName = project.AssemblyName;
                        IEnumerable<IFile> documentPaths = await project.Documents
                            .Where(x => !string.IsNullOrWhiteSpace(x.FilePath))
                            .SelectAsync(x => context.FileSystem.GetInputFileAsync(x.FilePath));
                        documentPaths = await documentPaths
                            .WhereAsync(async x => await x.GetExistsAsync() && (_whereFile == null || _whereFile(x)) && (_extensions?.Contains(x.Path.Extension) != false));
                        return documentPaths.Select(GetProjectDocument);

                        IDocument GetProjectDocument(IFile file)
                        {
                            Common.Tracing.Trace.Verbose($"Read file {file.Path.FullPath}");
                            return context.GetDocument(
                                file.Path,
                                null,
                                new MetadataItems
                                {
                                    { CodeAnalysisKeys.AssemblyName, assemblyName }
                                },
                                context.GetContentProvider(file));
                        }
                    }
                }
                return Array.Empty<IDocument>();
            }
        }
    }
}
