using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookConverter.Domain.Enums
{
    public enum ConversionStatus
    {
        Pending,
        ExtractingText,
        GeneratingAudio,
        MergingAudio,
        Completed,
        Failed
    }
}
