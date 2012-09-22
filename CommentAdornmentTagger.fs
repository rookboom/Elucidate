namespace Elucidate

open System
open System.ComponentModel.Composition
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Utilities

type CommentAdornment(view:IWpfTextView) =
    let layer = view.GetAdornmentLayer("Elucidate")
    let layoutChanged args = ()
    do view.LayoutChanged.Add(layoutChanged)
    member m.View = view

type CommentAdornmentTagger(view:ITextView(*, commentTagger*)) =
    let tagsChanged = new Event<EventHandler<SnapshotSpanEventArgs>, SnapshotSpanEventArgs>()
    let handleBufferChanged (sender:obj) (args:TextContentChangedEventArgs) = ()
    let handleLayoutChanged (sender:obj) (args:TextViewLayoutChangedEventArgs) = ()
    do view.LayoutChanged.AddHandler(fun sender args -> handleLayoutChanged sender args)
    do view.TextBuffer.Changed.AddHandler(fun sender args -> handleBufferChanged sender args)

    interface ITagger<IntraTextAdornmentTag> with
        [<CLIEvent>]
        member m.TagsChanged = tagsChanged.Publish
        member m.GetTags(spans) = Seq.empty

(* This export will expose our Comment Adornment Tagger Provider to Visual Studio *)
[<Export(typeof<IViewTaggerProvider>)>]
[<ContentType("text")>]
//[<ContentType("projection")>]
[<TagType(typeof<IntraTextAdornmentTag>)>]
type CommentAdornmentTaggerProvider() =
    [<Import>]
    let mutable BufferTagAggregatorFactoryService : IBufferTagAggregatorFactoryService = null
    
    interface IViewTaggerProvider with
            
        member m.CreateTagger<'T when 'T :> ITag>(textView:ITextView, buffer) : ITagger<'T>  = 
            let create() =
                let aggregator = BufferTagAggregatorFactoryService.CreateTagAggregator<CommentTag>(buffer)
                                |> box
                CommentAdornmentTagger(textView)
            try
                downcast(textView.Properties.GetOrCreateSingletonProperty<CommentAdornmentTagger>(
                                                 fun () -> create())|> box)
            with
            | e -> failwith e.Message

[<Export(typeof<IWpfTextViewCreationListener>)>]
[<ContentType("text")>]
[<TextViewRole(PredefinedTextViewRoles.Document)>]
type CommentAdornmentFactory() =
    [<Export(typeof<AdornmentLayerDefinition>)>]
    [<Name("Elucidate")>]
    [<Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)>]
    member m.AdornmentLayerDefinition editorAdornmentLayer = null;

    interface IWpfTextViewCreationListener with
        member m.TextViewCreated(textView) = 
            CommentAdornment(textView) |> ignore
