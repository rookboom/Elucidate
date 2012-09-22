namespace Elucidate

open System
open System.ComponentModel.Composition
open System.Windows.Controls
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Editor
open Microsoft.VisualStudio.Text.Tagging
open Microsoft.VisualStudio.Utilities

type CommentAdornment(commentTag:CommentTag) = 
    inherit FlowDocumentScrollViewer(
                IsToolBarVisible = false,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                IsSelectionEnabled = false,
                IsEnabled = false,
                IsManipulationEnabled = false)

        (*
        let update commentTag =
            var markdown = new Markdown();
            markdown.AutoNewLines = true;
            var html = markdown.Transform(commentTag.Comment);
            var xaml = HTMLConverter.HtmlToXamlConverter.ConvertHtmlToXaml(html, true);
            using (var stream = new MemoryStream(ASCIIEncoding.Default.GetBytes(xaml)))
            {
                Document = XamlReader.Load(stream) as FlowDocument;
                var converter = new BrushConverter();
                var brush = converter.ConvertFromString("white") as Brush;
                Document.Foreground = brush;
            }
            Document.IsEnabled = false;
            IsEnabled = false;*)


type CommentAdornmentTagger(view:ITextView, aggregator:ITagAggregator<CommentTag>) =
    let tagsChanged = new Event<EventHandler<SnapshotSpanEventArgs>, SnapshotSpanEventArgs>()
    let handleBufferChanged (sender:obj) (args:TextContentChangedEventArgs) = ()
    let handleLayoutChanged (sender:obj) (args:TextViewLayoutChangedEventArgs) = ()
    do view.LayoutChanged.AddHandler(fun sender args -> handleLayoutChanged sender args)
    do view.TextBuffer.Changed.AddHandler(fun sender args -> handleBufferChanged sender args)

    interface ITagger<IntraTextAdornmentTag> with
        [<CLIEvent>]
        member m.TagsChanged = tagsChanged.Publish
        member m.GetTags(spans) = 
            try

                let snapShot = spans.[0].Snapshot
                let tagSpan(mappingTagSpan:IMappingTagSpan<'T>) = 
                    let commentTag = mappingTagSpan.Tag
                    let snapShotSpan =
                        mappingTagSpan.Span.GetSpans(snapShot).[0]
                    TagSpan<IntraTextAdornmentTag>(snapShotSpan, 
                        IntraTextAdornmentTag(CommentAdornment(commentTag), null, Nullable(PositionAffinity.Predecessor)))
                aggregator.GetTags(spans)
                |> Seq.map tagSpan
                |> Seq.cast
            with
            | e -> failwith e.Message
            

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
