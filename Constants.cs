using System.Collections.Generic;
using System.Xml.Linq;

namespace vs_project_converter
{
    public static class Constants
    {
        public static readonly XNamespace Namespace = "http://schemas.microsoft.com/developer/msbuild/2003";
        
        public const string MSTestFramework = "Microsoft.VisualStudio.QualityTools.UnitTestFramework";
        public static readonly Dictionary<string,string> CoreMsTestReferences = new Dictionary<string,string>{
            {"Microsoft.NET.Test.Sdk", "15.3.0" },
            {"MSTest.TestAdapter", "1.1.18" },
            {"MSTest.TestFramework", "1.1.18" }
        };
        public static readonly string[] Imports = new []{
            "Microsoft.Common.props",
            "Microsoft.CSharp.targets",
            "Microsoft.WebApplication.targets",
            "Microsoft.TestTools.targets"
        };
        public static readonly Dictionary<string,string> TargetFrameworks = new Dictionary<string,string>
        {
            { "v4.5.1", "net451"},
            { "v4.5.2", "net452"},
            { "v4.6", "net46" }
        };

        public const string CoreStandardProject = "Microsoft.NET.Sdk";
        public const string CoreWebApplication = "Microsoft.NET.Sdk.Web";

        public const string WebApplicationTarget = "Microsoft.WebApplication.targets";
        public static class Elements
        {
            public const string TargetFrameworkVersion = "TargetFrameworkVersion";
            public const string TargetFramework = "TargetFramework";
            public const string Project = "Project";
            public const string Import = "Import";
            public const string Choose = "Choose";
            public const string Compile = "Compile";
            public const string EmbeddedResource = "EmbeddedResource";
            public const string Content = "Content";
            public const string None = "None";
            public const string ItemGroup = "ItemGroup";
            public const string RootNamespace = "RootNamespace";
            public const string AssemblyName = "AssemblyName";
            public const string Reference = "Reference";
            public const string HintPath = "HintPath";
            public const string PackageReference = "PackageReference";
            public const string ProjectReference = "ProjectReference";
            public static readonly string[] PropertyGroupElements = new []{
                "Configuration",
                "Platform",
                "OutputType",
                "AppDesignerFolder",
                "RootNamespace",
                "AssemblyName",
                "Optimize",
                "ProjectGuid",
                "ProjectTypeGuids",
                "FileAlignment",
                "OutputPath",
                "ErrorReport",
                "WarningLevel",
                "NuGetPackageImportStamp",
                "TargetFrameworkProfile",
                "TestProjectType",
                "IsCodedUITest",
                "ReferencePath",
                "VSToolsPath",
                "VisualStudioVersion"
            };
        }
        public static class Attributes
        {
            public const string Version = "Version";
            public const string Include = "Include";
            public const string Project = "Project";
            public const string Sdk = "Sdk";
            public const string Namespace = "xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"";
        }
    }
}