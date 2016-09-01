(*** hide ***)
namespace FSharp.Literate

open FSharp.Markdown
open System.IO
open System

(**
#Markdown Formatter
This class is a wrapper around the FSharp Literate programming tools. 
*)
type MDFormatter() =
(**
We find the runtime folder for the installed Visual Studio Extension through reflection
on any class in this assembly. This runtime folder contains:

- The html templates used for formatting the markdown that will be converted to
- The readme file that describes the project. This will be the default formatted text
  when the user is not editing a source code file yet.
*)
    let installationFolder =
        let packageType = typeof<MDFormatter>;
        let uri = Uri(packageType.Assembly.CodeBase)
        let assemblyFileInfo = FileInfo(uri.LocalPath)
        assemblyFileInfo.Directory.FullName

    let outputFile = sprintf @"%s\temp%A.html" installationFolder (Guid.NewGuid())
    let template = sprintf @"%s\Resources\template.html" installationFolder

    let readmeFile = installationFolder + "\README.md"    

(** We need a method to ignore the case of the file extension. This way we can match either *)
    let getExtensionLower file = Path.GetExtension(file).ToLower()

(**
The different file formats are each handled slightly differently:
For .fs code and .md markdown we just delegate to the literate programming tools to produce the output.
*)
    let processCode output file = 
        Literate.ProcessScriptFile(file, template, output, lineNumbers=false)
    let processMarkdown output file = 
        Literate.ProcessMarkdown(file, template, output, lineNumbers=false)
(** If the file extension is not known, we display the default page, which is markdown. *)
    let processUnknown output file = processMarkdown output readmeFile
(** For error messages we parse a markdown string providing additional information.*)
    let processError output msg = 
        let doc = Literate.ParseScriptString(msg)
        File.WriteAllText(output, Literate.WriteHtml(doc))
(** The tricky case is .fsx files that contain output values that need to be evaluated by the F# interpreter. 
To avoid incurring the overhead of the FSI if there is nothing to interpret, first read the text and scan
for occurrances of *include-output* and *include-value*. 

If there are such sections, we create an FSI Evaluator object and pass that along to the script parser.
If none exist, we treat it like any other code file. *)
    let processScript output file =
        let content = File.ReadAllText(file)
        if     content.Contains("(*** include-output") 
            || content.Contains("(*** include-value") then
            try
                let fsi = FsiEvaluator()
                let doc = Literate.ParseScriptString(content, fsiEvaluator = fsi)
                File.WriteAllText(output, Literate.WriteHtml(doc))
            with
            | e -> processError output (sprintf "(** **Error:** %s *)" e.Message)
        else
            processCode output file

(**
## Public methods

**Format** converts an F# source file to html using the F# literate programming tools.
Asynchronously returns *true* if it could successfully generate the html 
from the input text.Returns *false* if it takes more than 30 seconds to generate 
the html. An exception will be thrown in case of an error.
The resulting html is stored in **OutputFile**
**Note:** The script processing can take several seconds, especially if there is code that needs to be evaluated.
*)
    member m.Format(file) = 
            let output = outputFile
            file |> // Push the file to the function that matches the extension
            match getExtensionLower file with
            |".fs" ->  processCode output
            |".fsx" -> processScript output
            |".md" |".markdown" ->  processMarkdown output
            | _ -> processUnknown output
            output


(** Only *.fs*, *.fsx* and *.md*|*.markdown* files are supported *)
    member m.IsSupported(file) = 
        //ignore case
        match getExtensionLower file with
        | ".fs" | ".fsx" | ".md" | ".markdown" -> true | _ -> false

(** 
#Disposing
We need too delete the output file when we are done to prevent accumulating files in our plugin folder.
**_TODO: Find a way  to render  html to a string and still use templates for styling. This temp file
businness  is fragile._**
*)
    interface IDisposable  with
        member m.Dispose() =
            if File.Exists(outputFile) then
                File.Delete(outputFile)


