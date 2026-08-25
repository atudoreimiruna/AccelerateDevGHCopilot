using Library.ApplicationCore;
using Library.ApplicationCore.Entities;
using Library.ApplicationCore.Enums;
using Library.Console;
using Library.Infrastructure.Data;

public class ConsoleApp
{
    private ConsoleState _currentState = ConsoleState.PatronSearch;

    private List<Patron> _matchingPatrons = new();

    private Patron? _selectedPatron;
    private Loan? _selectedLoan;

    private readonly IPatronRepository _patronRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ILoanService _loanService;
    private readonly IPatronService _patronService;
    private readonly JsonData _jsonData;

    public ConsoleApp(
        ILoanService loanService,
        IPatronService patronService,
        IPatronRepository patronRepository,
        ILoanRepository loanRepository,
        JsonData jsonData)
    {
        _loanService = loanService;
        _patronService = patronService;
        _patronRepository = patronRepository;
        _loanRepository = loanRepository;
        _jsonData = jsonData;
    }

    public async Task Run()
    {
        while (_currentState != ConsoleState.Quit)
        {
            _currentState = await ExecuteCurrentState();
        }
    }

    private Task<ConsoleState> ExecuteCurrentState()
    {
        return _currentState switch
        {
            ConsoleState.PatronSearch => PatronSearch(),
            ConsoleState.PatronSearchResults => PatronSearchResults(),
            ConsoleState.PatronDetails => PatronDetails(),
            ConsoleState.LoanDetails => LoanDetails(),
            ConsoleState.Quit => Task.FromResult(ConsoleState.Quit),
            _ => throw new InvalidOperationException(
                $"Unhandled console state: {_currentState}")
        };
    }

    // ---------------------------------------------------------
    // Patron Search
    // ---------------------------------------------------------

    private async Task<ConsoleState> PatronSearch()
    {
        string searchInput = ReadPatronName();

        _matchingPatrons = await _patronRepository.SearchPatrons(searchInput);

        if (_matchingPatrons.Count == 0)
        {
            Console.WriteLine("No matching patrons found.");
            return ConsoleState.PatronSearch;
        }

        if (_matchingPatrons.Count > 20)
        {
            Console.WriteLine(
                "More than 20 patrons satisfy the search, " +
                "please provide more specific input...");

            return ConsoleState.PatronSearch;
        }

        Console.WriteLine("Matching Patrons:");
        PrintPatronsList(_matchingPatrons);

        return ConsoleState.PatronSearchResults;
    }

    private static string ReadPatronName()
    {
        while (true)
        {
            Console.Write("Enter a string to search for patrons by name: ");

            string? input = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input;
            }
        }
    }

    private static void PrintPatronsList(IEnumerable<Patron> patrons)
    {
        int number = 1;

        foreach (Patron patron in patrons)
        {
            Console.WriteLine($"{number}) {patron.Name}");
            number++;
        }
    }

    // ---------------------------------------------------------
    // Patron Search Results
    // ---------------------------------------------------------

    private async Task<ConsoleState> PatronSearchResults()
    {
        CommonActions options =
            CommonActions.Select |
            CommonActions.SearchPatrons |
            CommonActions.Quit;

        CommonActions action = ReadInputOptions(
            options,
            out int selectedPatronNumber);

        return action switch
        {
            CommonActions.Select =>
                await SelectPatron(selectedPatronNumber),

            CommonActions.SearchPatrons =>
                ConsoleState.PatronSearch,

            CommonActions.Quit =>
                ConsoleState.Quit,

            _ => throw new InvalidOperationException(
                $"Unhandled action: {action}")
        };
    }

    private async Task<ConsoleState> SelectPatron(int patronNumber)
    {
        if (patronNumber < 1 || patronNumber > _matchingPatrons.Count)
        {
            Console.WriteLine("Invalid patron number. Please try again.");
            return ConsoleState.PatronSearchResults;
        }

        Patron patron = _matchingPatrons[patronNumber - 1];

        _selectedPatron = await _patronRepository.GetPatron(patron.Id);

        if (_selectedPatron == null)
        {
            Console.WriteLine("Unable to load patron details.");
            return ConsoleState.PatronSearchResults;
        }

        return ConsoleState.PatronDetails;
    }

    // ---------------------------------------------------------
    // Patron Details
    // ---------------------------------------------------------

    private async Task<ConsoleState> PatronDetails()
    {
        if (_selectedPatron == null)
        {
            return ConsoleState.PatronSearch;
        }

        PrintPatronDetails(_selectedPatron);

        CommonActions options =
            CommonActions.SearchPatrons |
            CommonActions.Quit |
            CommonActions.Select |
            CommonActions.RenewPatronMembership |
            CommonActions.SearchBooks;

        CommonActions action = ReadInputOptions(
            options,
            out int selectedLoanNumber);

        return action switch
        {
            CommonActions.Select =>
                SelectLoan(selectedLoanNumber),

            CommonActions.SearchPatrons =>
                ConsoleState.PatronSearch,

            CommonActions.Quit =>
                ConsoleState.Quit,

            CommonActions.RenewPatronMembership =>
                await RenewMembership(),

            CommonActions.SearchBooks =>
                await SearchBooks(),

            _ => throw new InvalidOperationException(
                $"Unhandled action: {action}")
        };
    }

    private static void PrintPatronDetails(Patron patron)
    {
        Console.WriteLine($"Name: {patron.Name}");
        Console.WriteLine($"Membership Expiration: {patron.MembershipEnd}");
        Console.WriteLine();
        Console.WriteLine("Book Loans:");

        int loanNumber = 1;

        foreach (Loan loan in patron.Loans)
        {
            string returned = loan.ReturnDate != null
                ? "True"
                : "False";

            Console.WriteLine(
                $"{loanNumber}) " +
                $"{loan.BookItem?.Book?.Title} - " +
                $"Due: {loan.DueDate} - " +
                $"Returned: {returned}");

            loanNumber++;
        }
    }

    private ConsoleState SelectLoan(int loanNumber)
    {
        if (_selectedPatron == null)
        {
            return ConsoleState.PatronSearch;
        }

        if (loanNumber < 1 || loanNumber > _selectedPatron.Loans.Count)
        {
            Console.WriteLine("Invalid book loan number. Please try again.");
            return ConsoleState.PatronDetails;
        }

        _selectedLoan = _selectedPatron.Loans[loanNumber - 1];

        return ConsoleState.LoanDetails;
    }

    private async Task<ConsoleState> RenewMembership()
    {
        if (_selectedPatron == null)
        {
            return ConsoleState.PatronSearch;
        }

        var status = await _patronService.RenewMembership(
            _selectedPatron.Id);

        Console.WriteLine(EnumHelper.GetDescription(status));

        await ReloadSelectedPatron();

        return ConsoleState.PatronDetails;
    }

    // ---------------------------------------------------------
    // Loan Details
    // ---------------------------------------------------------

    private async Task<ConsoleState> LoanDetails()
    {
        if (_selectedLoan == null)
        {
            return ConsoleState.PatronDetails;
        }

        PrintLoanDetails(_selectedLoan);

        CommonActions options =
            CommonActions.SearchPatrons |
            CommonActions.Quit |
            CommonActions.ReturnLoanedBook |
            CommonActions.ExtendLoanedBook;

        CommonActions action = ReadInputOptions(options, out _);

        return action switch
        {
            CommonActions.ExtendLoanedBook =>
                await ExtendLoan(),

            CommonActions.ReturnLoanedBook =>
                await ReturnLoan(),

            CommonActions.SearchPatrons =>
                ConsoleState.PatronSearch,

            CommonActions.Quit =>
                ConsoleState.Quit,

            _ => throw new InvalidOperationException(
                $"Unhandled action: {action}")
        };
    }

    private static void PrintLoanDetails(Loan loan)
    {
        Console.WriteLine(
            $"Book title: {loan.BookItem?.Book?.Title}");

        Console.WriteLine(
            $"Book Author: {loan.BookItem?.Book?.Author?.Name}");

        Console.WriteLine($"Due date: {loan.DueDate}");

        Console.WriteLine(
            $"Returned: {(loan.ReturnDate != null).ToString()}");

        Console.WriteLine();
    }

    private async Task<ConsoleState> ExtendLoan()
    {
        if (_selectedLoan == null || _selectedPatron == null)
        {
            return ConsoleState.PatronSearch;
        }

        var status = await _loanService.ExtendLoan(
            _selectedLoan.Id);

        Console.WriteLine(EnumHelper.GetDescription(status));

        await ReloadSelectedPatron();
        await ReloadSelectedLoan();

        return ConsoleState.LoanDetails;
    }

    private async Task<ConsoleState> ReturnLoan()
    {
        if (_selectedLoan == null)
        {
            return ConsoleState.PatronDetails;
        }

        var status = await _loanService.ReturnLoan(
            _selectedLoan.Id);

        Console.WriteLine(EnumHelper.GetDescription(status));

        await ReloadSelectedLoan();

        return ConsoleState.LoanDetails;
    }

    // ---------------------------------------------------------
    // Search Books
    // ---------------------------------------------------------

    private async Task<ConsoleState> SearchBooks()
    {
        string bookTitle = ReadBookTitle();

        await _jsonData.EnsureDataLoaded();

        var book = _jsonData.Books?
            .FirstOrDefault(b =>
                string.Equals(
                    b.Title,
                    bookTitle,
                    StringComparison.OrdinalIgnoreCase));

        if (book == null)
        {
            Console.WriteLine(
                $"No book found with the title \"{bookTitle}\".");

            return ConsoleState.PatronDetails;
        }

        var bookItem = _jsonData.BookItems?
            .FirstOrDefault(bi => bi.BookId == book.Id);

        if (bookItem == null)
        {
            Console.WriteLine(
                $"No book item found for the title \"{book.Title}\".");

            return ConsoleState.PatronDetails;
        }

        var activeLoan = _jsonData.Loans?
            .FirstOrDefault(l =>
                l.BookItemId == bookItem.Id &&
                l.ReturnDate == null);

        if (activeLoan == null)
        {
            Console.WriteLine(
                $"\"{book.Title}\" is available for loan.");
        }
        else
        {
            Console.WriteLine(
                $"\"{book.Title}\" is on loan to another patron. " +
                $"The return due date is {activeLoan.DueDate}.");
        }

        return ConsoleState.PatronDetails;
    }

    private static string ReadBookTitle()
    {
        while (true)
        {
            Console.Write("Enter a book title to search for: ");

            string? input = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(input))
            {
                return input;
            }
        }
    }

    // ---------------------------------------------------------
    // Reload Helpers
    // ---------------------------------------------------------

    private async Task ReloadSelectedPatron()
    {
        if (_selectedPatron == null)
        {
            return;
        }

        _selectedPatron =
            await _patronRepository.GetPatron(_selectedPatron.Id);
    }

    private async Task ReloadSelectedLoan()
    {
        if (_selectedLoan == null)
        {
            return;
        }

        _selectedLoan =
            await _loanRepository.GetLoan(_selectedLoan.Id);
    }

    // ---------------------------------------------------------
    // Input Handling
    // ---------------------------------------------------------

    private static CommonActions ReadInputOptions(
        CommonActions options,
        out int optionNumber)
    {
        optionNumber = 0;

        while (true)
        {
            Console.WriteLine();
            WriteInputOptions(options);

            string? userInput = Console.ReadLine();

            CommonActions action = ParseInput(
                userInput,
                options,
                out optionNumber);

            if (action != CommonActions.Repeat)
            {
                return action;
            }

            Console.WriteLine("Invalid input. Please try again.");
        }
    }

    private static CommonActions ParseInput(
        string? input,
        CommonActions options,
        out int optionNumber)
    {
        optionNumber = 0;

        return input switch
        {
            "q" when options.HasFlag(CommonActions.Quit)
                => CommonActions.Quit,

            "s" when options.HasFlag(CommonActions.SearchPatrons)
                => CommonActions.SearchPatrons,

            "m" when options.HasFlag(CommonActions.RenewPatronMembership)
                => CommonActions.RenewPatronMembership,

            "e" when options.HasFlag(CommonActions.ExtendLoanedBook)
                => CommonActions.ExtendLoanedBook,

            "r" when options.HasFlag(CommonActions.ReturnLoanedBook)
                => CommonActions.ReturnLoanedBook,

            "b" when options.HasFlag(CommonActions.SearchBooks)
                => CommonActions.SearchBooks,

            _ when int.TryParse(input, out optionNumber)
                && options.HasFlag(CommonActions.Select)
                => CommonActions.Select,

            _ => CommonActions.Repeat
        };
    }

    private static void WriteInputOptions(CommonActions options)
    {
        Console.WriteLine("Input Options:");

        if (options.HasFlag(CommonActions.ReturnLoanedBook))
        {
            Console.WriteLine(" - \"r\" to mark as returned");
        }

        if (options.HasFlag(CommonActions.ExtendLoanedBook))
        {
            Console.WriteLine(" - \"e\" to extend the book loan");
        }

        if (options.HasFlag(CommonActions.RenewPatronMembership))
        {
            Console.WriteLine(" - \"m\" to extend patron's membership");
        }

        if (options.HasFlag(CommonActions.SearchPatrons))
        {
            Console.WriteLine(" - \"s\" for new search");
        }

        if (options.HasFlag(CommonActions.SearchBooks))
        {
            Console.WriteLine(" - \"b\" to check if a book is available for loan");
        }

        if (options.HasFlag(CommonActions.Quit))
        {
            Console.WriteLine(" - \"q\" to quit");
        }

        if (options.HasFlag(CommonActions.Select))
        {
            Console.WriteLine("Or type a number to select a list item.");
        }
    }
}
