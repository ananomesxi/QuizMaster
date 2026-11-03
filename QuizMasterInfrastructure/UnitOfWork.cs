using QuizMasterDomain.Interfaces;
using QuizMasterInfrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterInfrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly QuizMasterDbContext _context;
        public UnitOfWork(QuizMasterDbContext context)
        {
            _context = context;
        }
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
