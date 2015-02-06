using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace MarkdownEdit.Models
{
    public class MarkdownHighlightingColorizer : DocumentColorizingTransformer
    {
        public readonly Action<string> UpdateText;

        public MarkdownHighlightingColorizer()
        {
            UpdateText = Utility.Debounce<string>(Update);       
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            ChangeLinePart(line.Offset, line.EndOffset, 
                element => element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(Colors.DarkSeaGreen)));
        }

        private void Update(string text)
        {
            
        }
    }
}