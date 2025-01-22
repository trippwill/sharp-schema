using Json.Schema;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Tests.VerifyConverters;
using Xunit.Abstractions;

namespace SharpSchema.Generator.Tests;

public class AllFeaturesProjectTests : IClassFixture<AllFeaturesProjectFixture>
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly AllFeaturesProjectFixture _fixture;

    public AllFeaturesProjectTests(ITestOutputHelper outputHelper, AllFeaturesProjectFixture fixture)
    {
        _outputHelper = outputHelper;
        _fixture = fixture;

        VerifySettings = new VerifySettings();
        VerifySettings.AddExtraSettings(settings =>
        {
            settings.Converters.Add(new ISymbolJsonConverter());
            settings.Converters.Add(new EqualsValueClauseSyntaxConverter());
        });
    }

    public VerifySettings VerifySettings { get; }

    [Theory]
    [MemberData(nameof(VerifyAllFeaturesSchemaRootInfoOptions))]
    public async Task AllFeaturesProject_SchemaTree(OptionsProxy optionsProxy)
    {
        SchemaTreeGenerator.Options options = optionsProxy.Options;
        SchemaTree schemaTree = await GetSchemaTreeAsync(options);
        await Verify(schemaTree, this.VerifySettings).UseParameters(options);
    }

    [Fact]
    public async Task AllFeaturesProject_JsonSchema()
    {
        SchemaTree schemaTree = await GetSchemaTreeAsync();

        var jsonSchemaGenerator = new JsonSchemaGenerator();
        (JsonSchema jsonSchema, string? _) = jsonSchemaGenerator.Generate(schemaTree);

        string json = jsonSchema.SerializeToJson();
        _outputHelper.WriteLine(json);

        await Verify(json);
    }

    private async Task<SchemaTree> GetSchemaTreeAsync(SchemaTreeGenerator.Options? options = null)
    {
        var generator = new SchemaTreeGenerator(options);
        IReadOnlyCollection<SchemaTree> schemaRootInfos = await generator
            .FindRootsAsync(
                _fixture.Project!,
                CancellationToken.None);

        return schemaRootInfos.Single();
    }

    public class OptionsProxy : IXunitSerializable
    {
        public SchemaTreeGenerator.Options Options { get; private set; }

        public OptionsProxy()
        {
            Options = SchemaTreeGenerator.Options.Default;
        }

        public OptionsProxy(SchemaTreeGenerator.Options options)
        {
            Options = options;
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Options = new(
                new SchemaTreeGenerator.TypeOptions(
                    info.GetValue<AllowedTypeDeclarations>(nameof(Options.TypeOptions.AllowedTypeDeclarations)),
                    info.GetValue<AllowedAccessibilities>(nameof(Options.TypeOptions.AllowedAccessibilities))),
                new SchemaTreeGenerator.MemberOptions(
                    info.GetValue<AllowedAccessibilities>(nameof(Options.MemberOptions.AllowedAccessibilities))));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Options.TypeOptions.AllowedTypeDeclarations), Options.TypeOptions.AllowedTypeDeclarations);
            info.AddValue(nameof(Options.TypeOptions.AllowedAccessibilities), Options.TypeOptions.AllowedAccessibilities);
            info.AddValue(nameof(Options.MemberOptions.AllowedAccessibilities), Options.MemberOptions.AllowedAccessibilities);
        }

        public override string ToString() => Options.ToString();
    }

    public static TheoryData<OptionsProxy> VerifyAllFeaturesSchemaRootInfoOptions()
    {
        return new()
        {
            { new() },

            { new(new SchemaTreeGenerator.Options(
                new SchemaTreeGenerator.TypeOptions(
                    AllowedTypeDeclarations.Class,
                    AllowedAccessibilities.Public),
                new SchemaTreeGenerator.MemberOptions(
                    AllowedAccessibilities.Public))) },
        };
    }


}
