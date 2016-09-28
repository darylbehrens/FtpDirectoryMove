#r @"src/packages/FAKE/tools/FakeLib.dll"
open System
open System.IO
open Fake
open Fake.AssemblyInfoFile
open Fake.ZipHelper

let Build app guid version dir = 
    CreateCSharpAssemblyInfo ("src" </> app </> "Properties\AssemblyInfo.cs")
        [Attribute.Title       app
         Attribute.Description ""
         Attribute.Guid        guid
         Attribute.Product     app
         Attribute.Version     version
         Attribute.FileVersion version]
    let appReferences =
        !! "src\*.sln"
    MSBuildRelease "" "Build" appReferences
        |> Log "AppBuild-Output: "
    XCopy ("src" </> app </> @"bin\Release") dir

let Restore path =
    !! "src/**/packages.config"
        |> Seq.iter (RestorePackage (fun p -> { p with OutputPath = path }))

type Project = { Project : string; Version : string; }
let deployableApps = 
    !! "*VERSION.txt"
    |> Seq.map (fun v -> 
        let p = (fileNameWithoutExt v).Replace(".VERSION","")
        printfn "VERSION: %s" p
        { 
            Project = "src" </> p </> (sprintf "%s.csproj" p); 
            Version = File.ReadAllText(v).TrimEnd() 
        })
let toolPath = environVarOrDefault "TOOL_PATH" @"c:\dev\packages\"

let buildProject project =
    let app     = fileNameWithoutExt project.Project
    printfn "%s" app
    let guid    = "97caff3f-74a3-47fd-b487-9ae103367f1a"
    let deployPath = "deploy" </> app </> project.Version
    let dir     = deployPath </> "bin"

    CleanDir dir
    Build app guid project.Version dir

    printfn "%s" (dir </> (app + "." + project.Version + ".zip"))
    !! (dir </> "*")
    |> Zip deployPath (deployPath </> (app + "." + project.Version + ".zip")) 

Target "RestorePackages" (fun _ ->
    Restore "src/packages"
)

Target "Build" (fun _ ->
    deployableApps
    |> Seq.iter buildProject
)

"RestorePackages"
    ==> "Build"

RunTargetOrDefault "Build"