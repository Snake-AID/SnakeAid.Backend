using Microsoft.Extensions.Caching.Memory;
using SnakeAid.Core.Responses.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Service.Implements
{
    public class BankDirectoryService
    {
        private readonly IMemoryCache _cache;
        private const string BankCacheKey = "vietqr_banks";
        private readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

        public BankDirectoryService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<List<BankDirectoryResponse>> GetBanksAsync()
        {
            return await _cache.GetOrCreateAsync(BankCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                // In production, this would use VietQRHelper.BankApp.BanksObject
                // For now, return mock data
                var banks = new List<BankDirectoryResponse>
                {
                    new BankDirectoryResponse { Bin = "970400", Name = "Ngân hàng TMCP Sài Gòn Thương Tín (Sacombank)", VietQrStatus = VietQrStatus.TransferSupported },
                    new BankDirectoryResponse { Bin = "970405", Name = "Ngân hàng TMCP Quốc Tế (VIB)", VietQrStatus = VietQrStatus.TransferSupported },
                    new BankDirectoryResponse { Bin = "970418", Name = "Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV)", VietQrStatus = VietQrStatus.TransferSupported },
                    // Add more banks as needed
                };

                return banks;
            });
        }

        public async Task<BankDirectoryResponse> GetBankByBinAsync(string bin)
        {
            var banks = await GetBanksAsync();
            return banks.FirstOrDefault(b => b.Bin == bin);
        }
    }
}