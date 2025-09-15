using Antlr4.Runtime;
using System.Globalization;
using System.Text;

namespace FormulaValidator.Parsing
{
    /// <summary>
    /// Collects the first syntax error and formats it nicely for the UI.
    /// </summary>
    public sealed class CollectingErrorListener : BaseErrorListener
    {
        public bool HasError => _error is not null;
        public string? Error => _error;

        private string? _error;

        public override void SyntaxError(
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            if (_error != null) return;

            var tokenText = offendingSymbol?.Text ?? "EOF";
            var sb = new StringBuilder();

            // Try to convert ANTLR default error into more user-friendly messages
            if (tokenText == "<EOF>")
            {
                sb.Append("Unexpected end of formula");
            }
            else if (tokenText == ")")
            {
                sb.Append("Unexpected token: ')'");
            }
            else if (tokenText == "(")
            {
                sb.Append("Unexpected token: '('");
            }
            else
            {
                sb.Append($"Syntax error near '{tokenText}'");
            }

            sb.Append($" at [line {line}, col {charPositionInLine + 1}]");

            _error = sb.ToString();
        }
    }
}

