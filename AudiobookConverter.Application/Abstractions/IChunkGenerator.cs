using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookConverter.Application.Abstractions
{
    public interface IChunkGenerator
    {
        IReadOnlyList<string> SplitIntoChunks(string text, int maxChunkSize = 1000);
    }
}
