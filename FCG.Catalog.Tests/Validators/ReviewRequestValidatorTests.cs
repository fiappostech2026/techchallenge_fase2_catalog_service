using FCG.Catalog.Domain.Dto;
using FCG.Catalog.Domain.Validators;
using Xunit;

namespace FCG.Catalog.Tests.Validators;

public sealed class ReviewRequestValidatorTests
{
    private readonly ReviewRequestValidator _sut = new();

    [Fact]
    public void ReviewRequestValidator_ValidRequest_ValidationPasses()
    {
        var request = new ReviewRequest { Rating = 5, Comment = "Otimo" };

        var result = _sut.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void ReviewRequestValidator_RatingOutOfRange_Fails_WithRatingError(int rating)
    {
        var request = new ReviewRequest { Rating = rating };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReviewRequest.Rating));
    }

    [Fact]
    public void ReviewRequestValidator_CommentExceedsMaxLength_Fails_WithCommentError()
    {
        var request = new ReviewRequest { Rating = 3, Comment = new string('a', 2001) };

        var result = _sut.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReviewRequest.Comment));
    }

    [Fact]
    public void ReviewRequestValidator_EmptyComment_ValidationPasses()
    {
        var request = new ReviewRequest { Rating = 3, Comment = string.Empty };

        var result = _sut.Validate(request);

        Assert.True(result.IsValid);
    }
}
