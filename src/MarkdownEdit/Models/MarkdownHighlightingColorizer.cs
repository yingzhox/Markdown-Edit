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
                .Select(sb => new
                {
                    Block = sb,
                    SourcePosition = sb.Block.InlineContent?.SourcePosition ?? sb.Block.SourcePosition,
                    SourceLength = sb.Block.InlineContent?.SourceLength ?? sb.Block.SourceLength
                })
                .Where(a => a.SourcePosition <= end)
                .Where(a => a.SourcePosition + a.SourceLength > start);

            foreach (var astBlock in astBlocks)
            {
                switch (astBlock.Block.Block.Tag)
                {
                    case BlockTag.AtxHeader:
                    case BlockTag.SETextHeader:
                        ApplyLinePart(theme.HighlightHeading, astBlock.SourcePosition, astBlock.SourceLength, start, end, length);
                        break;

                    case BlockTag.BlockQuote:
                        ApplyLinePart(theme.HighlightBlockQuote, astBlock.SourcePosition, astBlock.SourceLength, start, end, length);
                        break;

                    case BlockTag.ListItem:
                        ApplyLinePart(theme.HighlightStrongEmphasis, astBlock.SourcePosition, astBlock.SourceLength, start, end, 1);
                        break;

                    case BlockTag.FencedCode:
                    case BlockTag.IndentedCode:
                        ApplyLinePart(theme.HighlightBlockCode, astBlock.SourcePosition, astBlock.SourceLength, start, end, length);
                        break;
                }
            }
        }

        private void ApplyLinePart(Highlight highlight, int sourceStart, int sourceLength, int lineStart, int lineEnd, int maxLength)
        {
            var start = Math.Max(sourceStart, lineStart);
            var end = Math.Min(lineEnd, start + Math.Min(sourceLength, maxLength));
            ChangeLinePart(start, end, element => ApplyHighlight(element, highlight));
        }

        public static void ApplyHighlight(VisualLineElement element, Highlight highlight)
        {
            var trp = element.TextRunProperties;

            var foregroundBrush = ColorBrush(highlight.Foreground);
            if (foregroundBrush != null) trp.SetForegroundBrush(foregroundBrush);

            var backgroundBrush = ColorBrush(highlight.Background);
            if (backgroundBrush != null) trp.SetForegroundBrush(backgroundBrush);

            var tf = element.TextRunProperties.Typeface;
            var weight = ConvertFontWeight(highlight.FontWeight) ?? tf.Weight;
            var style = ConvertFontStyle(highlight.FontStyle) ?? tf.Style;
            var typeFace = new Typeface(tf.FontFamily, style, weight, tf.Stretch);
            trp.SetTypeface(typeFace);

            if (highlight.Underline) trp.SetTextDecorations(TextDecorations.Underline);
        }

        private static Brush ColorBrush(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) return null;
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
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
            catch (NotSupportedException)
            {
                return null;
            }
        }

        public void OnTextChanged(string text)
        {
            _abstractSyntaxTree = ParseDocument(text);
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