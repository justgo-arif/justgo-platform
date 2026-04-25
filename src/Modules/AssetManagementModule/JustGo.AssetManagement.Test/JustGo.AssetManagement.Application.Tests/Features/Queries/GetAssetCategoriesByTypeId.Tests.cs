using Moq;
using TUnit;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Helper;
using JustGo.AssetManagement.Application.Features.AssetCategories.Queries.GetAssetCategories;
using JustGo.AssetManagement.Application.Features.Common.Queries.GetIdByGuid;
using TUnit.Core.Services;

namespace JustGo.AssetManagement.Test.JustGo.AssetManagement.Application.Tests.Features.Queries
{
    public class AssetCategoriesByTypeIdHandlerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IReadRepository<AssetCategory>> _readRepositoryMock;
        private readonly AssetCategoriesByTypeIdHandler _handler;

        public AssetCategoriesByTypeIdHandlerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _readRepositoryMock = new Mock<IReadRepository<AssetCategory>>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IReadRepository<AssetCategory>)))
                .Returns(_readRepositoryMock.Object);

            var lazyRepo = new LazyService<IReadRepository<AssetCategory>>(serviceProviderMock.Object);
            _handler = new AssetCategoriesByTypeIdHandler(lazyRepo, _mediatorMock.Object);
        }

        [Test]
        public async Task GetListOfAssetCategoriesByTypeId()
        {
            // Arrange
            var request = new GetAssetCategoriesByTypeIdQuery { AssetTypeId = "guid-1" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetIdByGuidQuery>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<int> { 100 });

            var fakeData = new List<AssetCategory>
            {
                new AssetCategory { RecordGuid = "cat-guid-1", Name = "Horse" },
                new AssetCategory { RecordGuid = "cat-guid-2", Name = "Gun" }
            };

            _readRepositoryMock.Setup(r => r.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
                .ReturnsAsync(fakeData);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            _readRepositoryMock.Verify(r => r.GetListAsync(
                    It.Is<string>(r => r.Contains(@"SELECT * FROM [dbo].[AssetCategories] WHERE AssetTypeId = ")),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    null,
                    "text"), Times.Once);

            await Assert.That(result).IsNotNull();
            await Assert.That(result.Count).IsEqualTo(2);
            await Assert.That(result[0].Name).IsEqualTo("Horse");
            await Assert.That(result[1].CategoryId).IsEqualTo("cat-guid-2");

            _mediatorMock.Verify(m => m.Send(It.IsAny<GetIdByGuidQuery>(), It.IsAny<CancellationToken>()), Times.Once);
            _readRepositoryMock.Verify(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"), Times.Once);
        }

        [Test]
        public async Task GetEmptyListOfAssetCategoriesByTypeIdWhileNoCategories()
        {
            // Arrange
            var request = new GetAssetCategoriesByTypeIdQuery { AssetTypeId = "guid-empty" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetIdByGuidQuery>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<int> { 200 });

            _readRepositoryMock.Setup(r => r.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
                .ReturnsAsync(new List<AssetCategory>());

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);


            // Assert
            _readRepositoryMock.Verify(r => r.GetListAsync(
                    It.Is<string>(r => r.Contains(@"SELECT * FROM [dbo].[AssetCategories] WHERE AssetTypeId = ")),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    null,
                    "text"), Times.Once);

            await Assert.That(result).IsNotNull();
            await Assert.That(result).IsEmpty();
        }
    }
}
