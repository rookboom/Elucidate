namespace Elucidate

open System
open System.Collections.Generic
open System.Linq
open System.Text.RegularExpressions
open System.ComponentModel.Composition
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Utilities

(*We need a container class that can hold the comments that are identified in the text.
This class has to implement the empty interface ITag just to be identified as a tag.*)
type CommentTag(comment:string) =
    interface ITag
    member m.Value = comment
    
(* We need a way to identify comments in our code. The CommentTagger will receive a text buffer
as input and will implement the ITagger interface which will provide a sequence of CommentTag
objects, each containing it's comment text. *)
type CommentTagger(buffer:ITextBuffer) as m =
    let matchExpressions = [|Regex(@"\(\*(?'xaml'(.|[\r\n])*?)\*\)", RegexOptions.Singleline)|]
    let tagsChanged = new Event<EventHandler<SnapshotSpanEventArgs>, SnapshotSpanEventArgs>()

    let handleBufferChanged (sender:obj) (args:TextContentChangedEventArgs) =
        if (args.Changes.Count > 0) then
            let snapshot = args.After
            let startPos = args.Changes.[0].NewPosition
            let endPos = args.Changes.[args.Changes.Count - 1].NewEnd

            let totalAffectedSpan = SnapshotSpan(
                                        snapshot.GetLineFromPosition(startPos).Start,
                                        snapshot.GetLineFromPosition(endPos).End)
            tagsChanged.Trigger(m, SnapshotSpanEventArgs(totalAffectedSpan))

    do buffer.Changed.AddHandler(fun sender args -> handleBufferChanged sender args)
    interface ITagger<CommentTag> with
        [<CLIEvent>]
        member m.TagsChanged = tagsChanged.Publish
        member m.GetTags(spans) =
            let documentSpan = spans.FirstOrDefault()
            let snapshot = documentSpan.Snapshot
            let text =  snapshot.GetText()
            let tagSpans(regex:Regex)= 
                let tagSpan(m:Match) = 
                    let tag =  CommentTag(m.Groups.["xaml"].Value) :> ITag
                    let span = SnapshotSpan(snapshot, m.Index, m.Length)
                    TagSpan(span, tag)
                regex.Matches(text).Cast<Match>()
                |> Seq.map tagSpan
                |> Seq.cast
            matchExpressions
            |> Seq.collect tagSpans
            
(* This export will expose our Comment Tagger Provider to Visual Studio *)
[<Export(typeof<ITaggerProvider>)>]
[<ContentType("text")>]
[<TagType(typeof<CommentTag>)>]
type CommentTaggerProvider() =
    interface ITaggerProvider with
        member m.CreateTagger<'T when 'T :> ITag>(buffer : ITextBuffer) : ITagger<'T> =
            downcast(buffer.Properties.GetOrCreateSingletonProperty<CommentTagger>(fun () -> CommentTagger(buffer)) |> box)
