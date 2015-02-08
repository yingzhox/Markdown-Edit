using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommonMark;
using CommonMark.Syntax;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace MarkdownEdit.Models
{
    public class MarkdownHighlightingColorizer : DocumentColorizingTransformer
    {
        private Block _abstractSyntaxTree;
        private Theme _theme;

        protected override void ColorizeLine(DocumentLine line)
        {
            var ast = _abstractSyntaxTree;
            if (ast == null) return;

            var theme = _theme;
            if (theme == null) return;

            var start = line.Offset;
            var end = line.EndOffset;
            var length = line.Length;

            var astBlocks = ast
                .AsEnumerable()
                .Where(sb => sb.Block != null)
                .Where(sb => sb.IsOpening)
                .Where(sb => sb.Block.Tag != BlockTag.Document)
                .Where(sb => sb.Block.SourcePosition < end)
                .Where(sb => (sb.Block.SourcePosition + sb.Block.SourceLength) > start);

            foreach (var block in astBlocks.Select(astBlock => astBlock.Block))
            {
                switch (block.Tag)
                {
                    case BlockTag.AtxHeader:
                    case BlockTag.SETextHeader:
                        ApplyLinePart(theme.HighlightHeading, start, length, start, end, length, 1.25);
                        break;

                    case BlockTag.BlockQuote:
                        ApplyLinePart(theme.HighlightBlockQuote, block.SourcePosition, block.SourceLength, start, end, length);
                        break;

                    case BlockTag.ListItem:
                        ApplyLinePart(theme.HighlightStrongEmphasis, block.SourcePosition, block.SourceLength, start, end, block.ListData.Padding);
                        break;

                    case BlockTag.FencedCode:
                    case BlockTag.IndentedCode:
                        ApplyLinePart(theme.HighlightBlockCode, block.SourcePosition, block.SourceLength, start, end, length);
                        break;
                }

                foreach (var inline in block
                    .AsEnumerable()
                    .Where(b => b.Inline != null)
                    .Select(inlineBlock => inlineBlock.Inline)
                    .Where(inline => inline.SourcePosition >= start && inline.SourcePosition < end))
                {
                    switch (inline.Tag)
                    {
                        case InlineTag.Code:
                            ApplyLinePart(theme.HighlightInlineCode, inline.SourcePosition, inline.SourceLength, start, end, inline.SourceLength);
                            break;

                        case InlineTag.Strong:
                            ApplyLinePart(theme.HighlightStrongEmphasis, inline.SourcePosition, inline.SourceLength, start, end, inline.SourceLength);
                            break;

                        case InlineTag.Image:
                            ApplyLinePart(theme.HighlightImage, inline.SourcePosition, inline.SourceLength, start, end, inline.SourceLength);
                            break;

                        case InlineTag.Link:
                            ApplyLinePart(theme.HighlightLink, inline.SourcePosition, inline.SourceLength, start, end, inline.SourceLength);
                            break;

                        case InlineTag.Emphasis:
                            ApplyLinePart(theme.HighlightEmphasis, inline.SourcePosition, inline.SourceLength, start, end, inline.SourceLength);
                            break;
                    }
                }
            }
        }

        private void ApplyLinePart(Highlight highlight, int sourceStart, int sourceLength, int lineStart, int lineEnd, int maxLength, double magnify = 1)
        {
            var start = Math.Max(sourceStart, lineStart);
            var end = Math.Min(lineEnd, start + Math.Min(sourceLength, maxLength));
            ChangeLinePart(start, end, element => ApplyHighlight(element, highlight, magnify));
        }

        private static void ApplyHighlight(VisualLineElement element, Highlight highlight, double magnify)
        {
            var trp = element.TextRunProperties;

            var foregroundBrush = ColorBrush(highlight.Foreground);
            if (foregroundBrush != null) trp.SetForegroundBrush(foregroundBrush);

            var backgroundBrush = ColorBrush(highlight.Background);
            if (backgroundBrush != null) trp.SetBackgroundBrush(backgroundBrush);

            var tf = element.TextRunProperties.Typeface;
            var weight = ConvertFontWeight(highlight.FontWeight) ?? tf.Weight;
            var style = ConvertFontStyle(highlight.FontStyle) ?? tf.Style;
            var typeFace = new Typeface(tf.FontFamily, style, weight, tf.Stretch);
            trp.SetTypeface(typeFace);

            if (highlight.Underline) trp.SetTextDecorations(TextDecorations.Underline);
            trp.SetFontRenderingEmSize(trp.FontRenderingEmSize * magnify);
        }

        private static Brush ColorBrush(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) return null;
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            }
            catch (FormatException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private static FontWeight? ConvertFontWeight(string fontWeight)
        {
            if (string.IsNullOrWhiteSpace(fontWeight)) return null;
            try
            {
                return (FontWeight)new FontWeightConverter().ConvertFromString(fontWeight);
            }
            catch (FormatException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private static FontStyle? ConvertFontStyle(string fontStyle)
        {
            if (string.IsNullOrWhiteSpace(fontStyle)) return null;
            try
            {
                return (FontStyle)(new FontStyleConverter().ConvertFromString(fontStyle));
            }
            catch (FormatException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        public void OnTextChanged(string text)
        {
            var doc = ParseDocument(text);
            // Possible CommonMark.Net bug: AtxHeader SourceLength is zero
            foreach (var item in doc.AsEnumerable().Where(item => item.Block != null && item.Block.Tag == BlockTag.AtxHeader)) item.Block.SourceLength = 1;
            _abstractSyntaxTree = doc;
        }

        public void OnThemeChanged(Theme theme)
        {
            _theme = theme;
        }

        private static Block ParseDocument(string text)
        {
            using (var reader = new StringReader(Normalize(text)))
            {
                var settings = new CommonMarkSettings {TrackSourcePosition = true};
                var doc = CommonMarkConverter.ProcessStage1(reader, settings);
                CommonMarkConverter.ProcessStage2(doc, settings);
                return doc;
            }
        }

        private static string Normalize(string value)
        {
            value = value.Replace('→', '\t');
            value = value.Replace('␣', ' ');
            return value;
        }
    }
}