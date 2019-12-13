#r "paket:
source ../FAKE/src/app/Fake.Sql.DacPac/bin/Debug
source https://api.nuget.org/v3/index.json
nuget Fake.IO.FileSystem
nuget Fake.Dotnet.MSBuild
nuget Fake.BuildServer.TeamCity
nuget Fake.Core.Target"
#load "./.fake/build.fsx/intellisense.fsx"

open System.Linq
open Fake.Core
open Fake.IO.Globbing.Operators
open Fake.DotNet

// Properties
let buildDir = "./build/"

// Helper methods
let getProjectName (fullPath: string) =
    fullPath.Split('\\').Last().Replace(".sqlproj", "")

let build (projectDirectoryPair:string * string) =
    let project, projectName = projectDirectoryPair
    MSBuild.runRelease  (fun p ->
        { p with Properties = [ ("DeployOnBuild", "false") ] } ) (buildDir + projectName + "/") "Build" (Seq.singleton project)
       |> Trace.logItems "DbBuild-Output: "

Target.create "BuildDb" (fun _ -> 

    !! "**/*.sqlproj" 
    |> Seq.map (fun x -> (x, getProjectName x))
    |> Seq.iter build
)

// start build
Target.runOrDefault "BuildDb"