#r "paket:
source https://api.nuget.org/v3/index.json //
nuget Fake.IO.FileSystem //
nuget Fake.Dotnet.MSBuild //
nuget Fake.Sql.SqlPackage //
nuget Fake.BuildServer.TeamCity //
nuget Fake.Core.Target //" 
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core.TargetOperators
open Fake.Core
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.DotNet
open System.Linq

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

let publish env (projectDirectoryPair:string * string) =
    let project, projectName = projectDirectoryPair
    MSBuild.runRelease  (fun p ->
        { p with Properties = [ ("DeployOnBuild", "true"); ("SqlPublishProfilePath","./Profiles/" + env + ".publish.xml") ] } ) (buildDir + projectName + "/") "Publish" (Seq.singleton project)
       |> Trace.logItems "DbDeploy-Output: "

let publishWithSqlPackageModule env (projectDirectoryPair:string * string) =
    let project, directory = projectDirectoryPair
    let dacPacPath = sprintf "./build/%s/%s.dacpac" directory directory
    let profile = sprintf "./%s/Profiles/%s.publish.xml" directory env

    Fake.Sql.SqlPackage.deployDb (fun args -> { args  with Source = dacPacPath; Profile = profile })

// Targets

Target.create "BuildDb" (fun _ -> 

    let db = Fake.Core.Environment.environVarOrDefault "db" "*"

    !! (sprintf "**/%s.sqlproj" db)
    |> Seq.map (fun x -> (x, getProjectName x))
    |> Seq.iter build
)

Target.create "DeployDb" (fun _ -> 

    let env = Fake.Core.Environment.environVarOrDefault "env" "Dev"
    let db = Fake.Core.Environment.environVarOrDefault "db" "*"

    !! (sprintf "**/%s.sqlproj" db)
    |> Seq.map (fun x -> (x, getProjectName x))
    |> Seq.iter (publishWithSqlPackageModule env)
)

Target.create "Clean" (fun _ ->
    Shell.cleanDir buildDir
)

"Clean"
    ==> "BuildDb"
    ==> "DeployDb"

// start build
Target.runOrDefault "DeployDb"