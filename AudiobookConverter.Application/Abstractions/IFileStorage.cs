using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookConverter.Application.Abstractions
{
    public interface IFileStorage
    {
        string GetInputPath(Guid conversionId, string originalFileName);
        string GetTempDirectory(Guid conversionId);
        string GetOutputPath(Guid conversionId);
    }
}
