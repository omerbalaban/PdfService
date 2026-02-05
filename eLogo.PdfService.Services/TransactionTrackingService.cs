using eLogo.LogProvider.Interface;
using eLogo.PdfService.Services.Domain.Models;
using eLogo.PdfService.Services.Interfaces;
using eLogo.PdfService.Services.Repositories;
using System;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services
{
    public class TransactionTrackingService : ITransactionTrackingService
    {
        private readonly ITransactionTracking _transactionRepository;
        private readonly IServiceLogger _logger;

        public TransactionTrackingService(ITransactionTracking transactionRepository, IServiceLogger logger)
        {
            _transactionRepository = transactionRepository;
            _logger = logger;
        }

        public async Task TrackRequestAsync(Domain.Models.TransactionTrackingModel transaction)
        {
            try
            {
                transaction.CreatedAt = DateTime.Now;
                await _transactionRepository.InsertAsync(transaction);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to track transaction", ex);
            }
        }

        public void TrackRequestFireAndForget(Domain.Models.TransactionTrackingModel transaction)
        {
            Task.Run(async () =>
            {
                try
                {
                    transaction.CreatedAt = DateTime.Now;
                    await _transactionRepository.InsertAsync(transaction);
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to track transaction in fire-and-forget mode", ex);
                }
            });
        }

        
    }
}
