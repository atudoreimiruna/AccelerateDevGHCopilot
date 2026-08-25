using Library.ApplicationCore;
using Library.ApplicationCore.Entities;
using Library.ApplicationCore.Enums;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;

    public const int ExtendByDays = 14;

    public LoanService(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<LoanReturnStatus> ReturnLoan(int loanId)
    {
        var loan = await _loanRepository.GetLoan(loanId);

        if (loan is null)
            return LoanReturnStatus.LoanNotFound;

        if (loan.ReturnDate is not null)
            return LoanReturnStatus.AlreadyReturned;

        loan.ReturnDate = DateTime.Now;

        return await TryUpdateLoan(loan)
            ? LoanReturnStatus.Success
            : LoanReturnStatus.Error;
    }

    public async Task<LoanExtensionStatus> ExtendLoan(int loanId)
    {
        var loan = await _loanRepository.GetLoan(loanId);

        if (loan is null)
            return LoanExtensionStatus.LoanNotFound;

        if (loan.Patron!.MembershipEnd < DateTime.Now)
            return LoanExtensionStatus.MembershipExpired;

        if (loan.ReturnDate is not null)
            return LoanExtensionStatus.LoanReturned;

        if (loan.DueDate < DateTime.Now)
            return LoanExtensionStatus.LoanExpired;

        loan.DueDate = loan.DueDate.AddDays(ExtendByDays);

        return await TryUpdateLoan(loan)
            ? LoanExtensionStatus.Success
            : LoanExtensionStatus.Error;
    }

    private async Task<bool> TryUpdateLoan(Loan loan)
    {
        try
        {
            await _loanRepository.UpdateLoan(loan);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
