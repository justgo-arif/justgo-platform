using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Features.MemberUpload.Queries.GetFileInformation;
using Moq;
using Xunit;

namespace JustGo.Result.Test.Features.GetFileInformation
{
    public class GetFileInformationQueryHandlerTest
    {
        [Theory]
        //[InlineData(1, 10, 2, 1, "TestFile.csv", 5, 2, 40, 60, false, "search")]
        [InlineData(1, 10, 0, 0, "", 0, 0, 0, 0, false, "")]
        //[InlineData(2, 1, 1, 1, "AnotherFile.csv", 10, 1, 10, 90, true, "")]
        public async Task Handle_ReturnsExpectedResults(
            int pageNumber,
            int pageSize,
            int totalCount,
            int expectedItemsCount,
            string expectedFileName,
            int records,
            int errors,     
            decimal errorPercentage,
            decimal successPercentage,
            bool hasMore,
            string search)
        {
            // Arrange
            var mockRepo = new Mock<IReadRepository<FileInformationDto>>();
            var mockFactory = new Mock<IReadRepositoryFactory>();

            var items = expectedItemsCount > 0
                ? new List<FileInformationDto>
                {
                    new FileInformationDto
                    {
                        FileId = 1,
                        FileName = expectedFileName,
                        Records = records,
                        Errors = errors,
                        ErrorPercentage = errorPercentage,
                        SuccessPercentage = successPercentage
                    }
                }
                : new List<FileInformationDto>();

            mockRepo.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    null,
                    "text"))
                .ReturnsAsync(items);

            // Setup GetSingleAsync to return totalCount as object (since repo returns object? for scalar)
            mockRepo.Setup(r => r.GetSingleAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    null,
                    "text"))
                .ReturnsAsync((object)totalCount);

            mockFactory.Setup(f => f.GetRepository<FileInformationDto>())
                .Returns(mockRepo.Object);

            var handler = new GetFileInformationQueryHandler(mockFactory.Object);

            var query = new GetFileInformationQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                OwnerId = 123,
                Search = search,
                SortBy = "File Name",
                OrderBy = "ASC"
            };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedItemsCount, result.Items.Count);
            Assert.Equal(totalCount, result.TotalCount);
            Assert.Equal(hasMore, result.HasMore);

            if (expectedItemsCount > 0)
            {
                var item = result.Items[0];
                Assert.Equal(expectedFileName, item.FileName);
                Assert.Equal(records, item.Records);
                Assert.Equal(errors, item.Errors);
                Assert.Equal(errorPercentage, item.ErrorPercentage);
                Assert.Equal(successPercentage, item.SuccessPercentage);
                Assert.Equal(item.FileId, result.LastSeenId);
            }
            else
            {
                Assert.Null(result.LastSeenId);
            }
        }
    }
}
