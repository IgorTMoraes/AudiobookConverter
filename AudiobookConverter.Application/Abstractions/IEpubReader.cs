using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudiobookConverter.Application.DTOs;

namespace AudiobookConverter.Application.Abstractions
{
    public interface IEpubReader
    {
        Task<BookContent> ReadAsync(Stream epubStream, CancellationToken cancellationToken = default);
    }
}
