namespace FSharp.Literate

(**
# First-level heading
Some more documentation using `Markdown`.
*)

(*** include: final-sample ***)

(** 
## Second-level heading
With some more documentation
*)

(*** define: final-sample ***)

open FSharp.Markdown
open System.IO
open System

module MDFormatter =
    type Dummy() =
        member m.Foo() = 42

    let private installationFolder =
        let packageType = typeof<Dummy>;
        let uri = Uri(packageType.Assembly.CodeBase)
        let assemblyFileInfo = FileInfo(uri.LocalPath)
        assemblyFileInfo.Directory.FullName

    let format(file) = 
        let output = sprintf @"%s\temp.html" installationFolder
        let template = sprintf @"%s\Resources\template.html" installationFolder
        Literate.ProcessScriptFile(file, template, output, lineNumbers=false)
        output

