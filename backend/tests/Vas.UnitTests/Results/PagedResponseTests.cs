using Vas.Shared.Results;

namespace Vas.UnitTests.Results;

public class PagedResponseTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(100, 30, 4)]
    public void TotalPages_rounds_up_to_cover_every_item(int totalCount, int pageSize, int expectedPages)
    {
        var response = new PagedResponse<int>([], totalCount, 1, pageSize);

        Assert.Equal(expectedPages, response.TotalPages);
    }

    [Fact]
    public void TotalPages_is_zero_when_page_size_is_not_positive()
    {
        var response = new PagedResponse<int>([], 50, 1, 0);

        Assert.Equal(0, response.TotalPages);
        Assert.False(response.HasNextPage);
    }

    [Fact]
    public void First_page_has_a_next_page_but_no_previous_page()
    {
        var response = new PagedResponse<int>([], 45, 1, 20);

        Assert.False(response.HasPreviousPage);
        Assert.True(response.HasNextPage);
    }

    [Fact]
    public void Last_page_has_a_previous_page_but_no_next_page()
    {
        var response = new PagedResponse<int>([], 45, 3, 20);

        Assert.True(response.HasPreviousPage);
        Assert.False(response.HasNextPage);
    }
}
