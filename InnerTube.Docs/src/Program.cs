using Microsoft.DocAsCode;
using Microsoft.DocAsCode.Dotnet;

namespace InnerTube.Docs;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await DotnetApiCatalog.GenerateManagedReferenceYamlFiles("./docfx.json");
        await Docset.Build("./docfx.json");
    }
}