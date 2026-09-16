using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookConverter.Application.Abstractions
{
    public interface ITextToSpeechEngine
    {
        Task SynthesizeAudioChunkAsync(string text, string outputWavPath, CancellationToken cancellationToken = default);
    }
}
