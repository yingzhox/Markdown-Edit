using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace MarkdownEdit.Models
{
    public class MarkdownHighlighter : IHighlighter
    {
        public IDocument Document { get; }
        public event HighlightingStateChangedEventHandler HighlightingStateChanged;

        public MarkdownHighlighter(IDocument document)
        {
            document.RequireNotNull();
            Document = document;
        }

        public HighlightedLine HighlightLine(int lineNumber)
        {
            var documentLine = Document.GetLineByNumber(lineNumber);
            return documentLine != null ? new HighlightedLine(Document, documentLine) : null;
        }

        public void UpdateHighlightingState(int lineNumber)
        {
        }

        public void BeginHighlighting()
        {
        }

        public void EndHighlighting()
        {
        }

        public HighlightingColor DefaultTextColor => null;

        public IEnumerable<HighlightingColor> GetColorStack(int lineNumber) => null;

        public HighlightingColor GetNamedColor(string name) => null;

        protected virtual void OnHighlightingStateChanged(int fromLineNumber, int toLineNumber)
        {
            HighlightingStateChanged?.Invoke(fromLineNumber, toLineNumber);
        }

        // IDisposable

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _disposed = true;
            }
        }
    }
}