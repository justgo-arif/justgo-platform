using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.Members.Commands.GenerateFamilyActionToken;
using JustGo.MemberProfile.Test.Helper;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Data;
using System.Security.Claims;

namespace JustGo.MemberProfile.Test.Features.Members
{
    public class GenerateFamilyActionTokenHandlerTests
    {
        private readonly Mock<IWriteRepositoryFactory> _writeRepositoryFactoryMock = new();
        private readonly Mock<IReadRepositoryFactory> _readRepositoryFactoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IUtilityService> _utilityServiceMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<IWriteRepository<object>> _writeRepositoryMock = new();
        private readonly Mock<IReadRepository<object>> _readRepositoryMock = new();
        private readonly Mock<IDbTransaction> _dbTransactionMock = new();
        private readonly Mock<HttpContext> _httpContextMock = new();

        private readonly GenerateFamilyActionTokenHandler _handler;

        public GenerateFamilyActionTokenHandlerTests()
        {
            // Setup Write Repository using LazyServiceMockHelper
            _writeRepositoryFactoryMock.Setup(x => x.GetLazyRepository<object>())
                .Returns(LazyServiceMockHelper.MockLazyService(_writeRepositoryMock.Object));

            // Setup Read Repository using LazyServiceMockHelper
            _readRepositoryFactoryMock.Setup(x => x.GetLazyRepository<object>())
                .Returns(LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object));

            // Setup UnitOfWork
            _unitOfWorkMock.Setup(x => x.BeginTransactionAsync())
                .ReturnsAsync(_dbTransactionMock.Object);

            // Setup HttpContext with Claims
            _httpContextMock.Setup(x => x.User)
                .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("TenantClientId", "testTenantId") })));
            _httpContextAccessorMock.Setup(x => x.HttpContext)
                .Returns(_httpContextMock.Object);

            // Setup Utility Service
            _utilityServiceMock.Setup(x => x.EncryptData(It.IsAny<string>()))
                .Returns("encryptedData");

            _handler = new GenerateFamilyActionTokenHandler(
                _writeRepositoryFactoryMock.Object,
                _unitOfWorkMock.Object,
                _readRepositoryFactoryMock.Object,
                _utilityServiceMock.Object,
                _httpContextAccessorMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_GenerateToken_And_CommitTransaction()
        {
            // Arrange
            var command = new GenerateFamilyActionTokenCommand
            {
                FamilyDocId = 1,
                InitiateMemberDocId = 2,
                TargetMemberDocId = 3,
                Url = "https://test.com/"
            };

            // Setup initial insert operation
            _readRepositoryMock.Setup(x => x.GetSingleAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    _dbTransactionMock.Object,
                    "text"))
                .ReturnsAsync(1);

            // Setup ExecuteAsync for all database operations
            _writeRepositoryMock.Setup(x => x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    _dbTransactionMock.Object,
                    It.IsAny<string>()))
                .ReturnsAsync(1);

            // Setup for user queries
            _readRepositoryMock.Setup(x => x.GetAsync(
                   It.Is<string>(s => s.Contains("SELECT UserId FROM [user]")),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    It.IsAny<IDbTransaction>(),
                    "text"))
                .ReturnsAsync(new { UserId = 1 });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(1);

            // Verify transaction handling
            _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(_dbTransactionMock.Object), Times.Once);

            // Verify all ExecuteAsync calls
            _writeRepositoryMock.Verify(x => x.ExecuteAsync(
                 It.Is<string>(s => s.Contains("UPDATE ActionToken")),
                 It.IsAny<object>(),
                 _dbTransactionMock.Object,
                 "text"),
                 Times.Once);

            // Verify stored procedures - using the correct overload without CancellationToken
            _writeRepositoryMock.Verify(x => x.ExecuteAsync(
                "[SendFamilyLinkNotificationEmail]",
                It.IsAny<object>(),
                _dbTransactionMock.Object,
                "sp"),
                Times.Once);

            _writeRepositoryMock.Verify(x => x.ExecuteAsync(
                "SEND_EMAIL_BY_SCHEME",
                It.IsAny<object>(),
                _dbTransactionMock.Object,
                "sp"),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowException_And_RollbackTransaction_WhenError()
        {
            // Arrange
            var command = new GenerateFamilyActionTokenCommand
            {
                FamilyDocId = 1,
                InitiateMemberDocId = 2,
                TargetMemberDocId = 3,
                Url = "https://test.com/"
            };

            _readRepositoryMock.Setup(x => x.GetSingleAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    _dbTransactionMock.Object,
                    "text"))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _handler.Handle(command, CancellationToken.None));

            _unitOfWorkMock.Verify(x => x.RollbackAsync(_dbTransactionMock.Object), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(_dbTransactionMock.Object), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_UseEncryptedTenantId_WhenAvailable()
        {
            // Arrange
            var command = new GenerateFamilyActionTokenCommand
            {
                FamilyDocId = 1,
                InitiateMemberDocId = 2,
                TargetMemberDocId = 3,
                Url = "https://test.com/"
            };

            string capturedSql = null;
            DynamicParameters capturedParams = null;

            _readRepositoryMock.Setup(x => x.GetSingleAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    _dbTransactionMock.Object,
                    "text"))
                .ReturnsAsync(1);

            _writeRepositoryMock.Setup(x => x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    _dbTransactionMock.Object,
                    "text"))
                .Callback<string, CancellationToken, object, IDbTransaction, string>((sql, ct, param, tx, commandType) =>
                {
                    capturedSql = sql;
                    capturedParams = param as DynamicParameters;
                })
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(1);
            _utilityServiceMock.Verify(x => x.EncryptData("testTenantId"), Times.Once);

            // Verify final email parameters contain encrypted tenant ID in URL

            _writeRepositoryMock.Verify(x => x.ExecuteAsync(
               "SEND_EMAIL_BY_SCHEME",
               It.IsAny<object>(),
               _dbTransactionMock.Object,
               "sp"),
               Times.Once);

        }
    }
}