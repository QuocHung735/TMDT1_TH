using TMDT1_TH.Domain.Entities;
using TMDT1_TH.Infrastructure.Marketplace;

namespace TMDT1_TH.Tests.Marketplace;

public sealed class StorePolicyTests
{
    [Fact]
    public void OfficialStore_CannotBeDeleted()
    {
        var result = StorePolicy.CanDelete(
            StoreDefaults.OfficialStoreId,
            0);

        Assert.False(result);
    }

    [Fact]
    public void StoreWithProducts_CannotBeDeleted()
    {
        var result = StorePolicy.CanDelete(
            StoreDefaults.OfficialStoreId + 1,
            3);

        Assert.False(result);
    }

    [Fact]
    public void EmptyNonOfficialStore_CanBeDeleted()
    {
        var result = StorePolicy.CanDelete(
            StoreDefaults.OfficialStoreId + 1,
            0);

        Assert.True(result);
    }

    [Fact]
    public void MissingReliabilityScore_HasNoDataLabel()
    {
        Assert.Equal(
            "Chưa đủ dữ liệu",
            StorePolicy.ReliabilityLabel(null));
    }

    [Theory]
    [InlineData(100, "Rất đáng tin cậy")]
    [InlineData(95, "Rất đáng tin cậy")]
    [InlineData(94, "Đáng tin cậy")]
    [InlineData(85, "Đáng tin cậy")]
    [InlineData(84, "Cần theo dõi")]
    [InlineData(75, "Cần theo dõi")]
    [InlineData(70, "Cần theo dõi")]
    [InlineData(69, "Rủi ro cao")]
    [InlineData(40, "Rủi ro cao")]
    public void ReliabilityLabel_IsMappedCorrectly(
        int score,
        string expected)
    {
        Assert.Equal(
            expected,
            StorePolicy.ReliabilityLabel(score));
    }
}