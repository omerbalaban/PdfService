using eLogo.LogProvider.Interface;
using eLogo.PdfService.Services.Domain.Models;
using eLogo.PdfService.Services.Interfaces;
using eLogo.PdfService.Services.Repositories;
using System;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services
{
    public class FailedConversionService : IFailedConversionService
    {
        private readonly IFailedConversionRepository _failedConversionRepository;
        private readonly IServiceLogger _logger;

        public FailedConversionService(IFailedConversionRepository failedConversionRepository, IServiceLogger logger)
        {
            _failedConversionRepository = failedConversionRepository;
            _logger = logger;
        }

        public async Task TrackFailedConversionAsync(FailedConversionModel failedConversion)
        {
            try
            {
                failedConversion.CreatedAt = DateTime.Now;
                await _failedConversionRepository.InsertAsync(failedConversion);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to track failed conversion", ex);
            }
        }

        public void TrackFailedConversionFireAndForget(FailedConversionModel failedConversion)
        {
            Task.Run(async () =>
            {
                try
                {
                    failedConversion.CreatedAt = DateTime.Now;
                    await _failedConversionRepository.InsertAsync(failedConversion);
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to track failed conversion in fire-and-forget mode", ex);
                }
            });
        }
    }
}
