# Testing Guidelines

- Tests must not read or write the user's real AppData, `%LOCALAPPDATA%`, home directory, or production ComicPlate data directory.
- Services that persist data must accept an injected storage root or path. Production code may call `CreateDefault()` to use the platform user data directory; tests must pass a unique temporary directory.
- Prefer `Path.Combine(Path.GetTempPath(), "ComicPlate.Tests", Guid.NewGuid().ToString("N"))` or the test runner's working directory for test storage.
- Do not request elevated or non-sandbox execution just to make tests pass. If a dependency tries to write outside the test directory, disable that behavior in test project configuration or redirect it to a temporary directory.
- Keep persistence tests isolated: each test class should own and clean up its temporary directory.
