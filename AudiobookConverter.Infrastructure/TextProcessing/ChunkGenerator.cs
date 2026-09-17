using AudiobookConverter.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookConverter.Infrastructure.TextProcessing
{
    public class ChunkGenerator : IChunkGenerator
    {
        public IReadOnlyList<string> SplitIntoChunks(string text, int maxChunkSize = 1000)
        {
            var chunks = new List<string>();
            var remaining = text.Trim();

            while (remaining.Length > maxChunkSize)
            {
                var cutPoint = FindCutPoint(remaining, maxChunkSize);
                chunks.Add(remaining[..cutPoint].Trim());
                remaining = remaining[cutPoint..].Trim();
            }

            if (remaining.Length > 0)
                chunks.Add(remaining);

            return chunks;
        }

        private static int FindCutPoint(string text, int maxSize)
        {
            var window = text[..Math.Min(maxSize, text.Length)];

            var lastPeriod = window.LastIndexOf('.');
            if (lastPeriod > maxSize / 2) return lastPeriod + 1;

            var lastComma = window.LastIndexOf(',');
            if (lastComma > maxSize / 2) return lastComma + 1;

            var lastSpace = window.LastIndexOf(' ');
            if (lastSpace > 0) return lastSpace + 1;

            return maxSize;
        }
    }
}
