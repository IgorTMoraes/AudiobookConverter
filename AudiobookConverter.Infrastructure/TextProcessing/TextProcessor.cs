using AudiobookConverter.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace AudiobookConverter.Infrastructure.TextProcessing
{
    public class TextProcessor : ITextProcessor
    {
        public string CleanAndNormalize(string rawTextOrHtml)
        {
            if (string.IsNullOrWhiteSpace(rawTextOrHtml))
                return string.Empty;

            // 1. Remover scripts e estilos CSS
            var text = Regex.Replace(rawTextOrHtml, @"<script[^>]*>[\s\S]*?</script>", string.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", string.Empty, RegexOptions.IgnoreCase);

            // 2. Substituir quebras de bloco HTML (<p>, <br>, <div>, <h1>-<h6>) por quebras de linha reais
            text = Regex.Replace(text, @"</?(p|div|h[1-6]|br)[^>]*>", "\n", RegexOptions.IgnoreCase);

            // 3. Remover quaisquer outras tags HTML
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);

            // 4. Decodificar caracteres especiais de HTML (&nbsp;, &amp;, &quot;, etc.)
            text = System.Net.WebUtility.HtmlDecode(text);

            // 5. Normalizar quebras de linha e espaços extras
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(line => line.Trim())
                            .Where(line => !string.IsNullOrWhiteSpace(line));

            return string.Join("\n\n", lines);
        }
    }
}
