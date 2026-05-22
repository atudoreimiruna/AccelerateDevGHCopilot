using Library.ApplicationCore;
using Library.ApplicationCore.Entities;
using Library.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Library.UnitTests.Infrastructure.JsonLoanRepositoryTests;

public class GetLoanTest
{
    private readonly ILoanRepository _mockLoanRepository;
    private readonly JsonLoanRepository _jsonLoanRepository;
    private readonly IConfiguration _configuration;
    private readonly JsonData _jsonData;

    public GetLoanTest()
    {
        _mockLoanRepository = Substitute.For<ILoanRepository>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JsonPaths:Authors", "Json/Authors.json" },
                { "JsonPaths:Books", "Json/Books.json" },
                { "JsonPaths:BookItems", "Json/BookItems.json" },
                { "JsonPaths:Patrons", "Json/Patrons.json" },
                { "JsonPaths:Loans", "Json/Loans.json" }
            })
            .Build();

        _jsonData = new JsonData(_configuration);
        _jsonLoanRepository = new JsonLoanRepository(_jsonData);
    }

    [Fact(DisplayName = "JsonLoanRepository.GetLoan: Returns loan when ID is found")]
    public async Task GetLoan_ReturnsLoanWhenIdFound()
    {
        // Arrange
        var expectedLoan = new Loan { Id = 1 };
        _mockLoanRepository.GetLoan(expectedLoan.Id).Returns(expectedLoan);

        // Act
        Loan? actualLoan = await _jsonLoanRepository.GetLoan(expectedLoan.Id);

        // Assert
        Assert.NotNull(actualLoan);
        Assert.Equal(expectedLoan.Id, actualLoan!.Id);
    }

    [Fact(DisplayName = "JsonLoanRepository.GetLoan: Returns null when ID is not found")]
    public async Task GetLoan_ReturnsNullWhenIdNotFound()
    {
        // Arrange
        int nonExistentId = 999;
        _mockLoanRepository.GetLoan(nonExistentId).Returns((Loan?)null);

        // Act
        Loan? actualLoan = await _jsonLoanRepository.GetLoan(nonExistentId);

        // Assert
        Assert.Null(actualLoan);
    }
}
