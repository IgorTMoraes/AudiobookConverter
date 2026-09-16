using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AudiobookConverter.Application.Jobs
{
    public class ConversionQueue
    {
        private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>();

        public ValueTask QueueConversionAsync(Guid conversionId)
        {
            return _queue.Writer.WriteAsync(conversionId);
        }

        public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
