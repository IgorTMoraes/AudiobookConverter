using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudiobookConverter.Domain.Enums;

namespace AudiobookConverter.Domain.Entities
{
    public class Conversion
    {
        public Guid Id { get; private set; }
        public string OriginalFileName { get; private set; }
        public string StoredFilePath { get; private set; }
        public string? OutputFilePath { get; set; }
        public ConversionStatus Status { get; private set; }
        public int Progress { get; private set; }
        public string? ErrorMessage { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public Conversion(string originalFileName, string storedFilePath)
        {
            Id = Guid.NewGuid();
            OriginalFileName = originalFileName;
            StoredFilePath = storedFilePath;
            Status = ConversionStatus.Pending;
            Progress = 0;
            CreatedAt = DateTime.UtcNow;
        }

        public void SetStoredFilePath(string path) => StoredFilePath = path;

        public void UpdateProgress(ConversionStatus status, int progress)
        {
            Status = status;
            Progress = Math.Clamp(progress, 0, 100);
        }

        public void Fail(string errorMessage)
        {
            Status = ConversionStatus.Failed;
            ErrorMessage = errorMessage;
        }
    }
}