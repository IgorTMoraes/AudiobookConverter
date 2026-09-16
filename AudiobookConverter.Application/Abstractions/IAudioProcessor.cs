using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookConverter.Application.Abstractions
{
    public interface IAudioProcessor
    {
        Task ConvertWavToMp3Async(string inputWavPath, string outputMp3Path, CancellationToken cancellationToken = default);
        Task MergeWavFilesAsync(IEnumerable<string> inputWavPaths, string outputMp3Path, CancellationToken cancellationToken = default);
    }
}
