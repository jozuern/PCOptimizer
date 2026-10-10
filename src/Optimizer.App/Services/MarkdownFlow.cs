using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MdBlock = Markdig.Syntax.Block;
using MdInline = Markdig.Syntax.Inlines.Inline;
using WpfBlock = System.Windows.Documents.Block;
using WpfInline = System.Windows.Documents.Inline;

namespace Optimizer.App.Services;

/// <summary>
/// Markdown -> FlowDocument: headings, paragraphs, lists, bold/italic, inline code and links only.
/// Links are shown as text with their URL.
/// </summary>
public static class MarkdownFlow
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().Build();

    public static FlowDocument Render(string markdown, Brush foreground, Brush secondary, Brush codeBackground, Brush line)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"),
            FontSize = 14.5,
            Foreground = foreground,
            PagePadding = new Thickness(0, 0, 12, 24),
            TextAlignment = TextAlignment.Left,
        };
        var ctx = new Ctx(secondary, codeBackground, line);
        foreach (var block in Markdown.Parse(markdown, Pipeline))
            if (Convert(block, ctx) is { } b) doc.Blocks.Add(b);
        return doc;
    }

    private sealed record Ctx(Brush Secondary, Brush CodeBackground, Brush Line);

    private static WpfBlock? Convert(MdBlock block, Ctx ctx)
    {
        switch (block)
        {
            case HeadingBlock { Level: <= 1 } h:
                // Title: display face, generous size
                var title = new Paragraph
                {
                    FontFamily = new FontFamily("Segoe UI Variable Display, Segoe UI"),
                    FontSize = 22,
                    FontWeight = FontWeights.SemiBold,
                    LineHeight = 28,
                    Margin = new Thickness(0, 4, 0, 10),
                };
                AddInlines(title.Inlines, h.Inline, ctx);
                return title;
            case HeadingBlock h:
                // Section: small caps overline with a hairline above
                var hp = new Paragraph
                {
                    FontSize = 12.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = ctx.Secondary,
                    BorderBrush = ctx.Line,
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Padding = new Thickness(0, 14, 0, 0),
                    Margin = new Thickness(0, 14, 0, 6),
                };
                Typography.SetCapitals(hp, FontCapitals.AllSmallCaps);
                AddInlines(hp.Inlines, h.Inline, ctx);
                return hp;
            case ParagraphBlock p:
                var para = new Paragraph { Margin = new Thickness(0, 0, 0, 9), LineHeight = 23 };
                AddInlines(para.Inlines, p.Inline, ctx);
                return para;
            case ListBlock l:
                var list = new List
                {
                    // Numbered steps keep their numbers; other lists have no marker (style rule: no bullet dots).
                    MarkerStyle = l.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.None,
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(l.IsOrdered ? 24 : 0, 0, 0, 0),
                };
                if (l.IsOrdered && int.TryParse(l.OrderedStart, out var start)) list.StartIndex = Math.Max(1, start);
                foreach (var item in l.OfType<ListItemBlock>())
                {
                    var li = new ListItem();
                    foreach (var child in item)
                        if (Convert(child, ctx) is { } c)
                        {
                            if (c is Paragraph cp) cp.Margin = new Thickness(0, 0, 0, l.IsOrdered ? 4 : 8);
                            li.Blocks.Add(c);
                        }
                    list.ListItems.Add(li);
                }
                return list;
            case ThematicBreakBlock:
                return new Paragraph(new Run(" ")) { BorderBrush = ctx.Line, BorderThickness = new Thickness(0, 0, 0, 1) };
            case LeafBlock leaf when leaf.Lines.Count > 0:
                return new Paragraph(new Run(leaf.Lines.ToString())) { FontFamily = new FontFamily("Cascadia Mono, Consolas"), Background = ctx.CodeBackground };
            default:
                return null;
        }
    }

    private static void AddInlines(InlineCollection target, ContainerInline? container, Ctx ctx)
    {
        if (container is null) return;
        foreach (var inline in container)
            if (Convert(inline, ctx) is { } wpf) target.Add(wpf);
    }

    private static WpfInline? Convert(MdInline inline, Ctx ctx)
    {
        switch (inline)
        {
            case LiteralInline lit:
                return new Run(lit.Content.ToString());
            case CodeInline code:
                return new Run(code.Content) { FontFamily = new FontFamily("Cascadia Mono, Consolas"), Background = ctx.CodeBackground, FontSize = 12 };
            case LineBreakInline:
                return new LineBreak();
            case EmphasisInline em:
                var span = em.DelimiterCount >= 2 ? (Span)new Bold() : new Italic();
                AddInlines(span.Inlines, em, ctx);
                return span;
            case LinkInline link when !link.IsImage:
                var text = new Span { Foreground = ctx.Secondary, TextDecorations = TextDecorations.Underline, ToolTip = link.Url };
                AddInlines(text.Inlines, link, ctx);
                if (text.Inlines.Count == 0) text.Inlines.Add(new Run(link.Url));
                return text;
            case AutolinkInline auto:
                return new Run(auto.Url) { Foreground = ctx.Secondary, FontSize = 12 };
            case DelimiterInline d:
                // An unmatched "[" (for example in "[1][2]", which Markdig first tries as a reference link) stays text.
                var ds = new Span(new Run(d.ToLiteral()));
                AddInlines(ds.Inlines, d, ctx);
                return ds;
            case ContainerInline c:
                var s = new Span();
                AddInlines(s.Inlines, c, ctx);
                return s;
            default:
                Debug.WriteLine($"Markdown inline not rendered: {inline.GetType().Name}");
                return null;
        }
    }
}
