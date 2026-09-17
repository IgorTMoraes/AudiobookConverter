using AudiobookConverter.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookConverter.Infrastructure.FileStorage
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _rootPath;

        public LocalFileStorage(IConfiguration configuration)
        {
            _rootPath = configuration["Storage:RootPath"]
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage");

            Directory.CreateDirectory(Path.Combine(_rootPath, "input"));
            Directory.CreateDirectory(Path.Combine(_rootPath, "temp"));
            Directory.CreateDirectory(Path.Combine(_rootPath, "output"));
        }

        public string GetInputPath(Guid conversionId, string originalFileName)
            => Path.Combine(_rootPath, "input", $"{conversionId}{Path.GetExtension(originalFileName)}");

        public string GetTempDirectory(Guid conversionId)
        {
            var dir = Path.Combine(_rootPath, "temp", conversionId.ToString());
            Directory.CreateDirectory(dir);
            return dir;
        }

        public string GetOutputPath(Guid conversionId)
            => Path.Combine(_rootPath, "output", $"{conversionId}.mp3");
    }
}
