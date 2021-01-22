#I @"./packages/build/FAKE/tools"

#r "FakeLib.dll"
#r "Newtonsoft.Json.dll"

open System.IO
open Fake
open Fake.GitVersionHelper
open Fake.AssemblyInfoFile

    module ScriptVars =
        let nugetKey() = "NUGET_API_KEY" |> environVarOrFail

        let nugetSource() = "https://www.nuget.org/api/v2/package"
        let version() = GitVersion (fun p -> { p with ToolPath = findToolInSubPath "GitVersion.exe" currentDirectory})

Target "Trace" <| fun _ ->
    ProcessHelper.enableProcessTracing <- false
    if DotNetCli.isInstalled() then
        tracefn "Dotnet CLI is installed. Version %s" <| DotNetCli.getVersion()
    else
        trace "Dotnet CLI is not installed. Compilation will fail."
    ProcessHelper.enableProcessTracing <- true

Target "Clean" <| fun _ ->
    let wipeDirs dirs = 
        dirs
        |> CleanDirs
        dirs
        |> DeleteDirs

    !! "artifacts"
    ++ "src/*/bin"
    ++ "tests/*/bin"
    ++ "src/*/obj"
    ++ "tests/*/obj"
    |> DeleteDirs

Target "SetVersion" <| fun _ ->
    let gitVersion = ScriptVars.version()
    let version = gitVersion.FullSemVer
    tracefn "Gitversion is %s" version

    CreateCSharpAssemblyInfo "SharedAssemblyInfo.cs"
        [Attribute.Version version
         Attribute.FileVersion version
         Attribute.InformationalVersion version
         Attribute.Company "Albelli"
         Attribute.Copyright "Albelli"
         Attribute.ComVisible false]

    // workaround for nuget publish, which doesn't
    // care whether the version is already set for the dll
    let searchPattern = "<Version>(.+?)<\/Version>"
    let newVersionTag = sprintf "<Version>%s</Version>" gitVersion.NuGetVersionV2

    !! "./src/**/*.csproj"
    ++ "./src/**/*.fsproj"
    |> RegexReplaceInFilesWithEncoding searchPattern newVersionTag System.Text.Encoding.UTF8

Target "Restore" <| fun _ ->
    DotNetCli.Restore id

Target "Compile" <| fun _ ->
    let buildProject project =
        DotNetCli.Build (fun p -> { p with Project = project})

    !! "src/**/*.csproj"
    ++ "src/**/*.fsproj"
    |> Seq.iter buildProject

Target "Package" <| fun _ ->
    let packageProject proj =
        tracefn "Packaging %s" proj
        DotNetCli.Pack (fun p ->
                        { p with
                              Project = proj
                              OutputPath = "artifacts" |> Path.GetFullPath
                              Configuration = "Release"
                              AdditionalArgs = ["--no-build"]
                              })

    !! "src/**/*.csproj"
    ++ "src/**/*.fsproj"
    |> Seq.iter packageProject

Target "PushNuget" <| fun _ ->
    let pushNugetPackageWithKey key source package =
        sprintf "nuget push -k %s -s %s %s" key source package
        |> DotNetCli.RunCommand id
    
    let pushPackage =
        pushNugetPackageWithKey (ScriptVars.nugetKey()) (ScriptVars.nugetSource())
    
    ProcessHelper.enableProcessTracing <- false                  
    !! "artifacts/*.nupkg"
    |> Seq.iter pushPackage
    ProcessHelper.enableProcessTracing <- true

"Trace"
==> "Clean"
==> "SetVersion"
==> "Restore"
==> "Compile"
==> "Package"
==> "PushNuget"

RunTargetOrDefault "Package"
