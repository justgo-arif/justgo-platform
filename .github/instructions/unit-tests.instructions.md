---
applyTo: "src/Modules/**/tests/**"
---

# Unit Test Conventions

Framework: xUnit + Moq + FluentAssertions.
Test location: `src/Modules/{Module}/tests/{Module}.UnitTests/Features/{Area}/`

## Handler test template

```csharp
public class MyHandlerTests
{
    private readonly Mock<IReadRepository<MyDto>> _repoMock;
    private readonly MyHandler _handler;

    public MyHandlerTests()
    {
        _repoMock = new Mock<IReadRepository<MyDto>>();
        var lazyRepo = LazyServiceMockHelper.MockLazyService(_repoMock.Object);
        _handler = new MyHandler(lazyRepo);
    }

    [Fact]
    public async Task Handle_ShouldReturn_WhenDataExists()
    {
        // Arrange
        var expected = new List<MyDto> { new() { Id = Guid.NewGuid() } };
        _repoMock.Setup(r => r.GetListAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>(),
                It.IsAny<object>(), null, "text"))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(new MyQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }
}
```

## Command test with transaction

```csharp
_unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(Mock.Of<IDbTransaction>());
_unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<IDbTransaction>())).Returns(Task.CompletedTask);

// After Act:
_unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
_unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Once);
```

## Key helper

`LazyServiceMockHelper.MockLazyService(mock.Object)` — always use this to wrap mocked repositories, never inject the mock directly.
