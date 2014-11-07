# Elucidate #

A Visual Studio extension that allows you to comment code using MarkDown, resulting in code that is interleaved with rich text, pictures and hyperlinks.

To use:

1. Install "Elucidate" from the Visual Studio gallery. 
2. Create either a markdown file (.md) or an F# script file (.fsx - [example](https://raw.githubusercontent.com/tpetricek/FSharp.Formatting/master/docs/content/sidebyside/script.fsx)) using MarkDown and literate scripting from [FSharp.Formatting](http://tpetricek.github.io/FSharp.Formatting/)
3. Open "View --> Other Windows --> Elucidate".  Save the script file. 

The Elucidate window should update with the rendered markdown. You may have to move in/out of the script window (Elucidate updates when the Elicudate window is open, and a script document is activated and then saved)

Elucidate was inspired by Donald Knuth's *literate programming*. Although it falls short of that ideal, the ability to anotate code with references and pictures aids in a better understanding of the code at hand. From my experience external code documentation is rarely created and consulted even less. The best place to document code is in the code. Unfortunately traditional comments falls short of allowing programmers to elaborate on the design. Sometimes a simple picture says a thousand words. The ability to link to other parts of the documentation is also key. As programmers we spend much more time reading code, than writing code. We should make this activity as effortless as possible for our fellow programmers.

#Known issues

- Slow updating after saving a file
	-	The current implementation relies on  FSharp.Literate which was not designed for real-time use.
	As such, updating the generated HTML takes slower than I would like. Hopefully this can be improved upon in the future. 
	For now this is what we have.
- F# Literal Evaluation syntax is not supported yet.
- The dark theme is currently the default. Mostly because I don't know the first thing about CSS. If you have a better understanding of this, you can edit the stylesheet in the plugin installation folder.

