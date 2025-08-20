using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace Skinde.Ui.Services;

public class CustomMarkdownExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        // No setup required for the pipeline
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            var linkRenderer = new CustomLinkRenderer();
            htmlRenderer.ObjectRenderers.Replace<LinkInlineRenderer>(linkRenderer);
        }
    }
}

public class CustomLinkRenderer : HtmlObjectRenderer<LinkInline>
{
    protected override void Write(HtmlRenderer renderer, LinkInline link)
    {
        if (link == null || string.IsNullOrEmpty(link.Url))
            return;

        if (link.Url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            renderer.Write("<a href=\"").Write(link.Url).Write("\">");
            //renderer.Write(link.FirstChild?.ToString()); // Remove "mailto:" from the displayed text
            renderer.WriteChildren(link);
            renderer.Write("</a>");
        }
        else if (link.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            renderer.Write("<a href=\"").Write(link.Url).Write("\" target=\"blank\">");
            renderer.WriteChildren(link);
            renderer.Write("</a>");
        }
        else
        {
            renderer.WriteChildren(link);
        }
    }
}