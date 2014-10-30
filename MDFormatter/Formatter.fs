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
on any class in this assembly. 
*)
    let installationFolder =
        let packageType = typeof<MDFormatter>;
        let uri = Uri(packageType.Assembly.CodeBase)
        let assemblyFileInfo = FileInfo(uri.LocalPath)
        assemblyFileInfo.Directory.FullName

    let output, template = 
        sprintf @"%s\temp.html" installationFolder,
        sprintf @"%s\Resources\template.html" installationFolder
    
(**
We need to control the requests for generating html since all request will write to the
same output file. A mailbox processor will act as a message queue to ensure only one 
message is processed at any given time.
*)
    let inbox = MailboxProcessor.Start(fun agent -> 
        let rec loop() = async {
            // Asynchronously wait for the next message
            let! file, (reply:AsyncReplyChannel<bool>) = agent.Receive()
            match Path.GetExtension(file) with
            |".fs" -> 
                Literate.ProcessScriptFile(file, template, output, lineNumbers=false)
            |".fsx" -> 
                let content = File.ReadAllText(file)
                //Avoid incurring the overhead of the FSI if there is nothing to interpret
                if     content.Contains("(*** include-output") 
                    || content.Contains("(*** include-value") then
                    let fsi = FsiEvaluator()
                    let doc = Literate.ParseScriptString(content, fsiEvaluator = fsi)
                    File.WriteAllText(output, Literate.WriteHtml(doc))
                else
                    let doc = Literate.ParseScriptString(content)
                    File.WriteAllText(output, Literate.WriteHtml(doc))
            |".md" -> 
                Literate.ProcessMarkdown(file, template, output, lineNumbers=false)
            | _ -> 
                Literate.ProcessMarkdown("Welcome.md", template, output, lineNumbers=false)
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

    member m.WelcomeFile = installationFolder + "\Welcome.md"

