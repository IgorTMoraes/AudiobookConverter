using AudiobookConverter.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudiobookConverter.Domain.Entities;

namespace AudiobookConverter.Infrastructure.Persistence.Repositories
{
    public class ConversionRepository : IConversionRepository
    {
        private readonly AppDbContext _context;

        public ConversionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Conversion conversion, CancellationToken cancellationToken = default)
        {
            await _context.Conversions.AddAsync(conversion, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Conversion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Conversions.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task UpdateAsync(Conversion conversion, CancellationToken cancellationToken = default)
        {
            _context.Conversions.Update(conversion);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
