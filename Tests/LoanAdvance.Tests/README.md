# LoanAdvance.Tests (Phase 17)

Unit & integration tests for the Loan & Advance module.

```
Tests/LoanAdvance.Tests/
  TestFixtures/LoanAdvanceTestFixture.cs   - seeds a minimal EF InMemory database + real UnitOfWork
  Unit/LoanCalculationServiceTests.cs      - EMI math (reducing balance + flat), pure, no DB
  Unit/LoanAdvanceValidatorsTests.cs       - FluentValidation validators (Loan side)
  Unit/AdvanceValidatorsTests.cs           - FluentValidation validators (Advance side)
  Integration/EmployeeLoanServiceTests.cs  - full Submit->Approve->Disburse->PreClose->Settle workflow
  Integration/EmployeeAdvanceServiceTests.cs - mirror, single-level approval
```

## Running

```
cd Tests/LoanAdvance.Tests
dotnet test
```

This repo has no `.sln` checked in; if you use one locally, add this project
with `dotnet sln add Tests\LoanAdvance.Tests\LoanAdvance.Tests.csproj`.

## Important caveat

**This test project was written but never executed.** The sandbox this was
authored in has no `dotnet` CLI available, so nothing here has been through
`dotnet restore`/`build`/`test`. Every entity/DTO/service shape referenced
was cross-checked against the actual source files, and the design (real
`UnitOfWork`/`Repository<T>` over EF Core's InMemory provider, so production
code runs unmodified against a fake data store) is deliberately chosen to
minimize the chance of a subtle behavioral mismatch versus mocking every
repository call individually - but there is a real, non-zero chance of a
compile error or an incorrect assumption about InMemory provider behavior
(most notably: InMemory does not enforce foreign-key referential integrity,
only "is this required scalar property null", which is why the fixture uses
placeholder ID strings for Company/Department/Designation/etc. instead of
seeding full rows for them).

**Run `dotnet test` as the first step after pulling this in**, and treat any
failure as a bug to fix, not a sign the approach is wrong - the individual
assertions were written directly against the real service/validator source,
so a failure most likely means either a genuine bug this test caught, or a
small fixture/setup mistake that needs a one-line fix.
