using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookConverter.Application.Abstractions
{
    public interface IAudioProcessor
    {
        Task ConvertWavToMp3Async(string inputWavPath, string outputMp3Path, CancellationToken cancellationToken);
        Task MergeWavFilesAsync(IEnumerable<string> wavFiles, string outputMp3Path, CancellationToken cancellationToken);
    }
}
