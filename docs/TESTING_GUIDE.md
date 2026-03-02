# Meridian Testing Guide

This document outlines the testing strategy for the Meridian platform, covering both backend Unit Tests (xUnit) and frontend UI Automation Tests (Selenium with NUnit). It also includes instructions for generating test coverage reports.

## Prerequisites

Before running any tests, ensure you have the following installed on your machine:
- **.NET SDK** (.NET 8.0 or the version specified in your projects)
- **Node.js & npm** (for running the frontend)
- **Google Chrome** (required for UI tests)

---

## 1. Backend Unit Tests (xUnit)

Unit tests are used to verify the business logic of individual microservices in isolation. These tests do not require the API, database, or frontend to be running.

### Running Unit Tests
You can run all unit tests across the backend solution using the .NET CLI.

**Mac & Windows:**
```bash
cd Meridian-Backend
dotnet test
```

*Note: This command will discover and run all `[Fact]` and `[Theory]` tests in any project configured as a test project.*

---

## 2. Frontend UI Automation Tests (Selenium + NUnit)

Our UI tests use Selenium WebDriver to automatically control a real Google Chrome browser, simulating user interactions like navigating pages, clicking, and verifying elements.

### Starting the Environment
UI tests require the frontend to be actively running because Selenium will attempt to navigate to `http://localhost:3000`.

**Mac & Windows:**
```bash
cd meridian-frontend
npm install   # If you haven't installed dependencies yet
npm run dev
```
*Leave this terminal running in the background.*

### Running the UI Tests

Open a **new terminal** and run the UI tests.

**Mac & Windows:**
```bash
cd meridian-frontend/UITests
dotnet test
```

### Visual Testing vs. Headless Mode
By default, the tests are configured to visibly launch Google Chrome so you can watch what the tests are doing. We've included artificial delays (`WaitBriefly()`) to make it easy to follow the automated actions.

**To run silently in the background (Headless Mode):**
If you want the tests to run faster without opening a visible browser window (e.g., for CI/CD pipelines), edit `UITests/BaseTest.cs` and uncomment the headless argument:

```csharp
options.AddArgument("--headless=new");
```

---

## 3. Test Coverage Reports

Code coverage reports show you exactly which lines of your code are being executed by your tests. This helps identify untested areas of the application. We use the `coverlet.collector` package (which is already installed in the test projects) to generate these metrics.

### Generating the Raw Report

To generate the coverage data, run `dotnet test` with the code coverage flag.

**Mac & Windows:**
```bash
# For backend unit tests:
cd Meridian-Backend
dotnet test --collect:"XPlat Code Coverage"

# For frontend UI tests:
cd meridian-frontend/UITests
dotnet test --collect:"XPlat Code Coverage"
```

This command runs the tests and generates an XML file (usually named `coverage.cobertura.xml`) inside a generated `TestResults/{guid}/` folder.

### Viewing a Beautiful HTML Report

Raw XML isn't easy to read. You can install the `.NET Global Tool` called **ReportGenerator** to convert this XML into an interactive HTML dashboard.

**Step 1: Install ReportGenerator (One-time setup for Mac & Windows):**
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

> **Troubleshooting `command not found`:**
> If you get an error saying `reportgenerator` is not found, your `.NET` global tools directory isn't in your system `PATH`.
> 
> **Mac Fix:** Run `echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.zshrc` (or `~/.bash_profile`), then restart your terminal.
> **Windows Fix:** Search the Start menu for "Environment Variables", edit the `Path` variable for your user account, and add `%USERPROFILE%\.dotnet\tools`. Restart your terminal.

**Step 2: Generate the HTML Report:**

**Mac:**
```bash
# Run this in the directory where you ran 'dotnet test'
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
open coveragereport/index.html
```

**Windows:**
```powershell
# Run this in the directory where you ran 'dotnet test'
reportgenerator -reports:"**\coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
start coveragereport\index.html
```

This will automatically open your default web browser displaying a comprehensive breakdown of your test coverage, allowing you to click into individual files to see exactly which lines were struck by your tests.

---

## 4. CI/CD Pipeline (Automated Testing)

Continuous Integration (CI) bridges the gap between running tests locally and ensuring code quality across the team. In the future, these testing workflows will be automated (e.g., via GitHub Actions). 

When someone opens a Pull Request or pushes to `develop`, the CI pipeline will automatically:
1. Spin up a fresh virtual machine.
2. Build the backend and run the `xUnit` Unit Tests.
3. Start the Next.js frontend in the background.
4. Run the Selenium UI Tests in **headless mode**.
5. Generate and publish the Coverage Report.
6. **Block the Pull Request** if any test fails or if the coverage drops below a defined threshold.
