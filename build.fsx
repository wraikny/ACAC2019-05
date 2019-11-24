#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

Target.initEnvironment ()

module Helper =
  let shellExec cmd args =
    let args = String.concat " " args

    Shell.Exec(cmd, args) |> function
    | 0 -> Trace.tracefn "Success '%s %s'" cmd args
    | code -> failwithf "Failed '%s %s', Exit Code: %d" cmd args code

  let runNetExe cmd args =
    if Environment.isWindows then
      shellExec cmd args
    else
      shellExec "mono" (cmd::args)

  let packResources target output password =
    let cmd = "./tool/FilePackageGenerator.exe"

    runNetExe cmd [
      yield target
      yield output

      match password with
      | Some(x) -> yield (sprintf "/k %s" x)
      | None -> ()
    ]

let targetProject = "TestApp"
let resources = "Resources"
let password = Some "password"

Target.create "Resources" (fun _ ->
  let outDir x = sprintf "src/%s/bin/%s/netcoreapp3.0" targetProject x

  // for Debug
  let dir = outDir "Debug"
  let target = dir + "/" + resources
  Directory.create dir
  Directory.delete target |> ignore
  Shell.copyDir target resources (fun _ -> true)
  Trace.trace "Finished Copying Resources for Debug"

  // for Release
  let dir = outDir "Release"
  let target = sprintf "%s/%s.pack" dir resources
  Directory.create dir
  Helper.packResources resources target password
  Trace.trace "Finished Packing Resources for Release"
)

Target.create "Clean" (fun _ ->
  !! "src/**/bin"
  ++ "src/**/obj"
  |> Shell.cleanDirs 
)

Target.create "Build" (fun _ ->
  !! "src/**/*.*proj"
  |> Seq.iter (DotNet.build id)
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "All"

Target.runOrDefault "All"
