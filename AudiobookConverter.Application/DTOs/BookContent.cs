using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookConverter.Application.DTOs
{
    public class BookContent
    {
        public string Title { get; init; } = string.Empty;
        public string? Author { get; init; }
        public IReadOnlyList<BookChapter> Chapters { get; init; } = [];
    }
}
