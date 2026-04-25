using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using Moq;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Features.MemberUpload.Queries.GetMemberData;

namespace JustGo.Result.Test.Features.GetMemberData
{
    public  class GetMemberDataByFileQueryHandlerTest
    {
        [Theory]
        [InlineData(1, 10, 100, 200, false, "search", "Member Id", "ASC")]
        [InlineData(2, 5, 101, 201, true, "", "Member Id", "DESC")]
        public async Task Handle_ReturnsPagedResult(
            int pageNumber, int pageSize, int ownerId, int fileId, bool errorsOnly, string search, string sortBy, string orderBy)
        {
            // Arrange
            var mockRepoFactory = new Mock<IReadRepositoryFactory>();
            var mockRepo = new Mock<IReadRepository<MemberDataDto>>();
            var handler = new GetMemberDataByFileQueryHandler(mockRepoFactory.Object);

            var query = new GetMemberDataByFileQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                OwnerId = ownerId,
                FileId = fileId,
                ErrorsOnly = errorsOnly,
                Search = search,
                SortBy = sortBy,
                OrderBy = orderBy
            };

            var expectedResult = new KeysetPagedResult<MemberDataDto>
            {
                Items =
                [
                    new MemberDataDto
                    {
                        FileName = null!,
                        MemberData = null!
                    }
                ],
                HasMore = false
            };

            mockRepoFactory.Setup(f => f.GetRepository<MemberDataDto>())
                .Returns(mockRepo.Object);

            mockRepo.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    null,
                    It.IsAny<string>()))
                .ReturnsAsync(expectedResult.Items);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);

        }
    }
}
