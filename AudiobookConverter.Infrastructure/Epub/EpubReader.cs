using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudiobookConverter.Application.Abstractions;
using AudiobookConverter.Application.DTOs;
using VersOne.Epub;

namespace AudiobookConverter.Infrastructure.Epub
{
    public class EpubReader : IEpubReader
    {
        private readonly ITextProcessor _textProcessor;

        public EpubReader(ITextProcessor textProcessor)
        {
            _textProcessor = textProcessor;
        }

        public async Task<BookContent> ReadAsync(Stream epubStream, CancellationToken cancellationToken = default)
        {
            // 1. Abrir o arquivo EPUB usando a biblioteca VersOne.Epub
            var book = await VersOne.Epub.EpubReader.ReadBookAsync(epubStream);

            var chapters = new List<BookChapter>();
            int order = 1;

            // 2. Mapear a tabela de conteúdo (Navigation / TOC) para tentar obter os nomes dos capítulos
            var navigationItems = book.Navigation ?? new List<EpubNavigationItem>();

            // 3. Percorrer a ordem de leitura dos arquivos de texto do livro
            foreach (var textContentFile in book.ReadingOrder)
            {
                var rawContent = textContentFile.Content;

                // Limpar HTML e normalizar texto do capítulo
                var cleanText = _textProcessor.CleanAndNormalize(rawContent);

                if (string.IsNullOrWhiteSpace(cleanText))
                    continue;

                // Tenta encontrar o título do capítulo no mapa de navegação pelo nome do arquivo
                var matchedNav = navigationItems.FirstOrDefault(n =>
                    n.Link != null && n.Link.ContentFilePath == textContentFile.FilePath);

                string chapterTitle = matchedNav?.Title ?? $"Capítulo {order}";

                chapters.Add(new BookChapter
                {
                    Order = order++,
                    Title = chapterTitle,
                    Text = cleanText
                });
            }

            return new BookContent
            {
                Title = string.IsNullOrWhiteSpace(book.Title) ? "Livro sem título" : book.Title,
                Author = book.Author,
                Chapters = chapters
            };
        }
    }
}
