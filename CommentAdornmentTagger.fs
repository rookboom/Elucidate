namespace Elucidate

open System
open System.IO
open System.Text
open System.Collections.Generic
open System.ComponentModel.Composition
open System.Windows.Controls
open System.Windows.Documents
open System.Windows.Markup
open System.Windows.Shapes
open System.Windows.Media
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Utilities
open MarkdownSharp

type CommentAdornment(comment) as this = 
    inherit FlowDocumentScrollViewer(
                IsToolBarVisible = false,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                IsSelectionEnabled = false,
                IsEnabled = false,
                IsManipulationEnabled = false)
        
    let init =
        let flowDocument =
            let markdown = new Markdown(AutoNewLines = true)
            let html = markdown.Transform(comment)
            let xaml = HTMLConverter.HtmlToXamlConverter.ConvertHtmlToXaml(html, true)
            let bytes = ASCIIEncoding.Default.GetBytes(xaml)
            use stream = new MemoryStream(bytes)
            let doc = XamlReader.Load(stream) :?> FlowDocument
            doc.Foreground <- 
                BrushConverter().ConvertFromString("white") :?> Brush
            doc
        this.Document <- flowDocument


type CommentAdornmentTagger(view:ITextView, aggregator:ITagAggregator<CommentTag>) =
    let adornmentCache = Dictionary<string, CommentAdornment>()
    let tagsChanged = new Event<EventHandler<SnapshotSpanEventArgs>, SnapshotSpanEventArgs>()
    let handleBufferChanged (sender:obj) (args:TextContentChangedEventArgs) =
        let change = args.Changes.Item(0)
        let oldText = change.OldText
        let markdown = 
            if (oldText.Length > 4) then
                oldText.Substring(2, oldText.Length-4)
            else
                ""
        let deletingFlowDoc = markdown.Length > 0
                           && change.NewLength - change.OldLength < -1 
                           && adornmentCache.ContainsKey(markdown)
        if (deletingFlowDoc) then
            // When deleting a flow document, we want to replace it with the original
            // markdown, enclosed in a block comment without the final close bracket. (Otherwise it will just be
            // detected as a markdown region and re-transformed into a FlowDocument)
            // This will allow the user to edit the markdown, and close the bracket
            // when finished to show the result.
            adornmentCache.Remove(markdown) |> ignore
            let edit = view.TextBuffer.CreateEdit()
            edit.Insert(change.NewPosition, sprintf "(*%s*" markdown) |> ignore
            edit.Apply() |> ignore
        ()


    let handleLayoutChanged (sender:obj) (args:TextViewLayoutChangedEventArgs) = ()
    do view.LayoutChanged.AddHandler(fun sender args -> handleLayoutChanged sender args)
    do view.TextBuffer.Changed.AddHandler(fun sender args -> handleBufferChanged sender args)
    let createAdornment(comment) =
        match adornmentCache.TryGetValue(comment) with
        | true, adornment -> adornment
        | false, _ ->
            let adornment = CommentAdornment(comment)
            adornmentCache.Add(comment, adornment)
            adornment

    interface ITagger<IntraTextAdornmentTag> with
        [<CLIEvent>]
        member m.TagsChanged = tagsChanged.Publish
        member m.GetTags(spans) = 
            // We only ever deal with a single span match. This is because
            // we transform our matched text to a FlowDocument and we will never
            // match the source text again. Need to confirm that this holds true
            // the first time we parse the document
            let tagSpan(mappingTagSpan:IMappingTagSpan<CommentTag>) = 
                let snapShot = spans.[0].Snapshot
                let commentTag = mappingTagSpan.Tag
                let snapShotSpan =
                    mappingTagSpan.Span.GetSpans(snapShot).[0]
                TagSpan<IntraTextAdornmentTag>(snapShotSpan, 
                    IntraTextAdornmentTag(createAdornment(commentTag.Value), null, Nullable(PositionAffinity.Predecessor)))
            aggregator.GetTags(spans)
            |> Seq.map tagSpan
            |> Seq.cast
            

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
                CommentAdornmentTagger(textView, aggregator)
            try
                downcast(textView.Properties.GetOrCreateSingletonProperty<CommentAdornmentTagger>(
                                                 fun () -> create())|> box)
            with
            | e -> failwith e.Message

            (*
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
            CommentAdornment(textView) |> ignore *)
