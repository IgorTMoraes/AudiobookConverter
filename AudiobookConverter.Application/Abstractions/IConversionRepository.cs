using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudiobookConverter.Domain.Entities;

namespace AudiobookConverter.Application.Abstractions
{
    public interface IConversionRepository
    {
        Task AddAsync(Conversion conversion, CancellationToken cancellationToken = default);
        Task<Conversion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task UpdateAsync(Conversion conversion, CancellationToken cancellationToken = default);
    }
}
