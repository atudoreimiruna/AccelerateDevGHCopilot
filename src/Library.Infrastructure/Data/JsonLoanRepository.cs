using Library.ApplicationCore;
using Library.ApplicationCore.Entities;

namespace Library.Infrastructure.Data;

public class JsonLoanRepository : ILoanRepository
{
    private readonly JsonData _jsonData;

    public JsonLoanRepository(JsonData jsonData)
    {
        ArgumentNullException.ThrowIfNull(jsonData);
        _jsonData = jsonData;
    }

    public async Task<Loan?> GetLoan(int id)
    {
        await _jsonData.EnsureDataLoaded();

        Loan? loan = _jsonData.Loans?
            .FirstOrDefault(l => l.Id == id);

        return loan == null
            ? null
            : _jsonData.GetPopulatedLoan(loan);
    }

    public async Task UpdateLoan(Loan loan)
    {
        ArgumentNullException.ThrowIfNull(loan);

        await _jsonData.EnsureDataLoaded();

        Loan? existingLoan = _jsonData.Loans?
            .FirstOrDefault(l => l.Id == loan.Id);

        if (existingLoan == null)
        {
            return;
        }

        UpdateLoanProperties(existingLoan, loan);

        await _jsonData.SaveLoans(_jsonData.Loans!);
        await _jsonData.LoadData();
    }

    private static void UpdateLoanProperties(
        Loan existingLoan,
        Loan updatedLoan)
    {
        existingLoan.BookItemId = updatedLoan.BookItemId;
        existingLoan.PatronId = updatedLoan.PatronId;
        existingLoan.LoanDate = updatedLoan.LoanDate;
        existingLoan.DueDate = updatedLoan.DueDate;
        existingLoan.ReturnDate = updatedLoan.ReturnDate;
    }
}
