using TMDT1_TH.Domain.Entities;

namespace TMDT1_TH.Infrastructure.Marketplace;

public static class StorePolicy
{
    public static bool CanDelete(
        int storeId,
        int productCount)
    {
        return storeId !=
                   StoreDefaults.OfficialStoreId &&
               productCount == 0;
    }

    public static string ReliabilityLabel(
        decimal? score)
    {
        if (!score.HasValue)
            return "Chưa đủ dữ liệu";

        return score.Value switch
        {
            >= 95m => "Rất đáng tin cậy",
            >= 85m => "Đáng tin cậy",
            >= 70m => "Cần theo dõi",
            _ => "Rủi ro cao"
        };
    }
}
