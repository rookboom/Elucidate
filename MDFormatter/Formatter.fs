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

    let output, template = 
        sprintf @"%s\temp.html" installationFolder,
        sprintf @"%s\Resources\template.html" installationFolder

    let readmeFile = installationFolder + "\README.md"    

(** We need a method to ignore the case of the file extension. This way we can match either *)
    let getExtensionLower file = Path.GetExtension(file).ToLower()

(**
The different file formats are each handled slightly differently:
For .fs code and .md markdown we just delegate to the literate programming tools to produce the output.
*)
    let processCode file = 
        Literate.ProcessScriptFile(file, template, output, lineNumbers=false)
    let processMarkdown file = 
        Literate.ProcessMarkdown(file, template, output, lineNumbers=false)
(** If the file extension is not known, we display the default page, which is markdown. *)
    let processUnknown file = processMarkdown readmeFile
(** For error messages we parse a markdown string providing additional information.*)
    let processError msg = 
        let doc = Literate.ParseScriptString(msg)
        File.WriteAllText(output, Literate.WriteHtml(doc))
(** The tricky case is .fsx files that contain output values that need to be evaluated by the F# interpreter. 
To avoid incurring the overhead of the FSI if there is nothing to interpret, first read the text and scan
for occurrances of *include-output* and *include-value*. 

If there are such sections, we create an FSI Evaluator object and pass that along to the script parser.
If none exist, we treat it like any other code file. *)
    let processScript file =
        let content = File.ReadAllText(file)
        if     content.Contains("(*** include-output") 
            || content.Contains("(*** include-value") then
            try
                let fsi = FsiEvaluator()
                let doc = Literate.ParseScriptString(content, fsiEvaluator = fsi)
                File.WriteAllText(output, Literate.WriteHtml(doc))
            with
            | e -> processError(sprintf "(** **Error:** %s *)" e.Message)
        else
            processCode file

(**
The script processing can take several seconds, especially if there is code that need to be evaluated.
We need to control the requests for generating html since there is only a single output file. 
A mailbox processor will act as a message queue to ensure only one message is processed at any given time.*)

    let inbox = MailboxProcessor.Start(fun agent -> 
        let rec loop() = async {
            // Asynchronously wait for the next message
            let! file, (reply:AsyncReplyChannel<bool>) = agent.Receive()
            file |> // Push the file to the function that matches the extension
            match getExtensionLower file with
            |".fs" ->  processCode
            |".fsx" -> processScript
            |".md" ->  processMarkdown
            | _ -> processUnknown
            reply.Reply(true)
            return! loop() }
        loop())
(**
## Public methods
*)
    /// The output file where the generated html for the current document will be saved.
    member m.OutputFile = output

(**
**Format** converts an F# source file to html using the F# literate programming tools.
Asynchronously returns *true* if it could successfully generate the html 
from the input text.Returns *false* if it takes more than 30 seconds to generate 
the html. An exception will be thrown in case of an error.
The resulting html is stored in **OutputFile**
*)
    member m.Format(file) = 
        async {
            let buildMessage ch = file, ch
            let seconds = 1000
            let! rsp = inbox.PostAndTryAsyncReply<bool>(buildMessage, timeout=30*seconds)
            return match rsp with
                   | None -> false
                   | Some(success) -> success
        } |> Async.StartAsTask

(** Only *.fs*, *.fsx* and *.md* files are supported *)
    member m.IsSupported(file) = 
        //ignore case
        match getExtensionLower file with
        | ".fs" | ".fsx" | ".md" -> true | _ -> false

