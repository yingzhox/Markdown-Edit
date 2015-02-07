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

            var blocks = ast
                .AsEnumerable()
                .Where(sb => sb.IsOpening)
                .Where(sb => sb.Block != null && sb.Block.SourcePosition >= start)
                .Where(sb => sb.Block != null && sb.Block.SourcePosition <= end);

            foreach (var block in blocks)
            {
                if (block.Block.Tag == BlockTag.AtxHeader)
                {
                    ChangeLinePart(block.Block.InlineContent.SourcePosition, block.Block.InlineContent.SourcePosition + block.Block.InlineContent.SourceLength,
                        element => ApplyHighlight(element, theme.HighlightHeading));
                }
            }
        }

        public static void ApplyHighlight(VisualLineElement element, Highlight highlight)
        {
            var trp = element.TextRunProperties;
            var tf = element.TextRunProperties.Typeface;

            var foregroundBrush = ColorBrush(highlight.Foreground);
            if (foregroundBrush != null) trp.SetForegroundBrush(foregroundBrush);

            trp.SetFontRenderingEmSize(trp.FontRenderingEmSize * 1.2);
            var weight = ConvertFontWeight(highlight.FontWeight, tf.Weight);
            var style = ConvertFontStyle(highlight.FontStyle, tf.Style);
            var typeFace = new Typeface(tf.FontFamily, style, weight, tf.Stretch);
            trp.SetTypeface(typeFace);
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

        private static FontWeight ConvertFontWeight(string fontWeight, FontWeight defaultWeight)
        {
            if (string.IsNullOrWhiteSpace(fontWeight)) return defaultWeight;
            try
            {
                return (FontWeight)new FontWeightConverter().ConvertFromString(fontWeight);
            }
            catch (NotSupportedException)
            {
                return defaultWeight;
            }
        }

        private static FontStyle ConvertFontStyle(string fontStyle, FontStyle defaultStyle)
        {
            if (string.IsNullOrWhiteSpace(fontStyle)) return defaultStyle;
            try
            {
                return (FontStyle)(new FontStyleConverter().ConvertFromString(fontStyle));
            }
            catch (NotSupportedException)
            {
                return defaultStyle;
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