using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookConverter.Application.DTOs
{
    public class BookChapter
    {
        public int Order { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
    }
}
