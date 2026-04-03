using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SnakeAid.Core.Responses.Wallet;
using VietQRHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class BankDirectoryService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<BankDirectoryService> _logger;
        private const string BankCacheKey = "vietqr_banks";
        private readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

        public BankDirectoryService(IMemoryCache cache, ILogger<BankDirectoryService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<BankDirectoryResponse>> GetBanksAsync()
        {
            return await _cache.GetOrCreateAsync(BankCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                await Task.CompletedTask;

                try
                {
                    return BankApp.BanksObject.Values
                        .Select(b => new BankDirectoryResponse
                        {
                            Key = b.key,
                            Code = b.code,
                            ShortName = b.shortName ?? b.name,
                            Name = b.name ?? string.Empty,
                            Bin = b.bin ?? string.Empty,
                            VietQrStatus = MapStatus(b.vietQRStatus),
                            LookupSupported = b.lookupSupported > 0,
                            SwiftCode = string.IsNullOrWhiteSpace(b.swiftCode) ? null : b.swiftCode
                        })
                        .OrderBy(b => b.ShortName ?? b.Name)
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load bank directory from VietQRHelper.");
                    return new List<BankDirectoryResponse>();
                }
            }) ?? new List<BankDirectoryResponse>();
        }

        public async Task<BankDirectoryResponse?> GetBankByBinAsync(string bin)
        {
            var banks = await GetBanksAsync();
            return banks.FirstOrDefault(b => b.Bin == bin);
        }

        private static VietQrStatus MapStatus(int status)
        {
            return status switch
            {
                (int)VietQRStatus.TRANSFER_SUPPORTED => VietQrStatus.TransferSupported,
                (int)VietQRStatus.RECEIVE_ONLY => VietQrStatus.ReceiveOnly,
                _ => VietQrStatus.ReceiveOnly
            };
        }
    }
}
