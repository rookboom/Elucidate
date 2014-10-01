// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

#r "../packages/FSharp.Formatting.2.4.0/lib/net40/FSharp.Markdown.dll"
open FSharp.Markdown

// Define your library scripting code here

let text = @"(**
# First-level heading
Some.*)"

let doc = Markdown.Parse(text)
let html = Markdown.WriteHtml(doc)
