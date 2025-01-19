using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using SharpSchema.Generator;

using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace SharpSchema.Build;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public sealed class GenerateSchemaTask : MSBuildTask, ICancelableTask, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public ITaskItem[]? Compile { get; set; }

    public ITaskItem[]? ReferencePath { get; set; }

    public ITaskItem[]? ProjectReference { get; set; }

    public string? SchemaOutputDirectory { get; set; }

    [Output]
    public string? SchemaOutputFileName { get; set; }

    public void Cancel() => _cancellationTokenSource.Cancel();

    public override bool Execute()
    {
        this.Log.LogMessage("Building analysis project...");

        AdhocWorkspace workspace = this.CreateAdhocWorkspace();

        this.Log.LogMessage("Generating schema...");

        Task<bool> result = Task.Run(async () =>
        {
            try
            {
                await Task.Yield();
                //this.SchemaOutputFileName = await new SchemaRootInfoGenerator()
                //    .FindSchemaRootTypesAsync(
                //        workspace,
                //        _cancellationTokenSource.Token);

                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                this.Log.LogErrorFromException(ex, showStackTrace: true);
                return false;
            }
        });

        this.Log.LogMessage(MessageImportance.High, $"Schema output file: {this.SchemaOutputFileName}");
        return result.Result;
    }

    private AdhocWorkspace CreateAdhocWorkspace()
    {
        AdhocWorkspace workspace = new(MefHostServices.DefaultHost);

        ProjectInfo projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "SchemaAnalysisProject",
            "SchemaAnalysisProject",
            LanguageNames.CSharp);

        Project project = workspace.AddProject(projectInfo);

        // Set parse options for the project
        CSharpParseOptions parseOptions = new CSharpParseOptions(documentationMode: DocumentationMode.Parse);
        project = project.WithParseOptions(parseOptions);

        if (this.Compile is not null)
        {
            foreach (ITaskItem item in this.Compile)
            {
                SourceText sourceText = SourceText.From(File.ReadAllText(item.ItemSpec));
                workspace.AddDocument(project.Id, Path.GetFileName(item.ItemSpec), sourceText);
            }
        }

        if (this.ReferencePath is not null)
        {
            foreach (ITaskItem item in this.ReferencePath)
            {
                PortableExecutableReference metadataReference = MetadataReference.CreateFromFile(item.ItemSpec);
                project = project.AddMetadataReference(metadataReference);
            }
        }

        if (this.ProjectReference is not null)
        {
            foreach (ITaskItem item in this.ProjectReference)
            {
                ProjectReference projectReference = new ProjectReference(ProjectId.CreateNewId(item.ItemSpec));
                project = project.AddProjectReference(projectReference);
            }
        }

        return workspace;
    }

    public void Dispose() => _cancellationTokenSource.Dispose();
}
