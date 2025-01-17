using System.Collections.Generic;
using System.IO;
using System.Linq;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Statiq.Common.Configuration;
using Statiq.Common.Execution;
using Statiq.Common.IO;
using Statiq.Common.Meta;

namespace Statiq.CodeAnalysis
{
    /// <summary>
    /// Reads all the source files from a specified msbuild solution.
    /// This module will be executed once and input documents will be ignored if a search path is
    /// specified. Otherwise, if a delegate is specified the module will be executed once per input
    /// document and the resulting output documents will be aggregated.
    /// Note that this requires the MSBuild tools to be installed (included with Visual Studio).
    /// </summary>
    /// <remarks>
    /// The output of this module is similar to executing the ReadFiles module on all source files in the solution.
    /// </remarks>
    /// <metadata cref="CodeAnalysisKeys.AssemblyName" usage="Output" />
    /// <metadata cref="CodeAnalysisKeys.OutputBuildLog" usage="Setting"/>
    /// <category>Input/Output</category>
    public class ReadSolution : ReadWorkspace
    {
        /// <summary>
        /// Reads the solution file at the specified path. This allows you to specify a different solution file depending on the input.
        /// </summary>
        /// <param name="path">A delegate that returns a <c>FilePath</c> with the solution file path.</param>
        public ReadSolution(DocumentConfig<FilePath> path)
            : base(path)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<Project> GetProjects(IExecutionContext context, IFile file)
        {
            StringWriter log = new StringWriter();
            AnalyzerManager manager = new AnalyzerManager(file.Path.Directory.FullPath, new AnalyzerManagerOptions
            {
                LogWriter = log
            });

            AnalyzerResult[] results = manager.Projects.Values
                .Select(analyzer =>
                {
                    if (context.Bool(CodeAnalysisKeys.OutputBuildLog))
                    {
                        analyzer.AddBinaryLogger();
                    }
                    return CompileProjectAndTrace(analyzer, log);
                })
                .Where(x => x != null)
                .ToArray();

            AdhocWorkspace workspace = new AdhocWorkspace();
            foreach (AnalyzerResult result in results)
            {
                result.AddToWorkspace(workspace);
            }
            return workspace.CurrentSolution.Projects;
        }
    }
}