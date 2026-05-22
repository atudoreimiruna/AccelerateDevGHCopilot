# Library App

## Description
The Library App is a console-based application designed to manage a library system. It provides functionality for managing books, patrons, and loans, with a modular architecture that separates concerns across different layers.

## Project Structure
The project is organized into the following structure:
- **src/**
  - **Library.ApplicationCore/**
    - `Entities/`: Contains the core domain entities such as `Author`, `Book`, `Loan`, and `Patron`.
    - `Enums/`: Defines enumerations like `LoanExtensionStatus` and `MembershipRenewalStatus`.
    - `Interfaces/`: Contains abstractions for repositories and services.
    - `Services/`: Implements business logic for managing loans and patrons.
  - **Library.Console/**
    - `Program.cs`: The entry point of the application.
    - `ConsoleApp.cs`: Handles the main application flow and user interactions.
    - `CommonActions.cs`: Provides reusable actions for the console interface.
    - `appSettings.json`: Stores configuration settings for the application.
  - **Library.Infrastructure/**
    - `Data/`: Contains data access classes like `JsonData`, `JsonLoanRepository`, and `JsonPatronRepository`.

## Key Classes and Interfaces
- **Entities**:
  - `Author`, `Book`, `Loan`, `Patron`: Represent the core objects in the library system.
- **Repositories**:
  - `IPatronRepository`, `ILoanRepository`: Define data access contracts.
  - `JsonPatronRepository`, `JsonLoanRepository`: Implement data access using JSON files.
- **Services**:
  - `ILoanService`, `IPatronService`: Define business logic contracts.
  - `LoanService`, `PatronService`: Implement business logic for loans and patrons.
- **ConsoleApp**:
  - Manages the user interface and application flow.

## Usage
1. Clone the repository to your local machine.
2. Open the project in Visual Studio Code.
3. Build the solution to restore dependencies.
4. Run the application by executing the `Program.cs` file.
5. Follow the on-screen instructions to interact with the library system.

## License
This project is licensed under the MIT License. See the LICENSE file for details.