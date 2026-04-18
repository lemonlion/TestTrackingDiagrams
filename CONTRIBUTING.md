# Contributing to TestTrackingDiagrams

Thank you for your interest in contributing! This document provides guidelines for contributing to the project.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/<your-username>/TestTrackingDiagrams.git`
3. Create a branch: `git checkout -b my-feature`
4. Make your changes
5. Push and open a Pull Request

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or later, `global.json` controls roll-forward)
- A PlantUML server for diagram rendering (optional — tests use the public server by default)

## Development Workflow

This project follows **Test-Driven Development (TDD)**:

1. **Red** — Write a failing test
2. **Green** — Write the minimum code to make it pass
3. **Refactor** — Improve the code while keeping tests green

### Building

```bash
dotnet build TestTrackingDiagrams.sln
```

### Running Tests

```bash
# Core tests
dotnet test tests/TestTrackingDiagrams.Tests

# All tests (excluding Selenium)
dotnet test TestTrackingDiagrams.sln --filter "FullyQualifiedName!~Selenium"
```

### Project Structure

```
src/          — Library source packages
tests/        — Test projects
examples/     — Example.Api integration examples
```

## Pull Request Guidelines

- Follow existing code style (enforced by `.editorconfig`)
- Include tests for new functionality
- Update documentation if the public API changes
- Keep PRs focused — one feature or fix per PR

## Reporting Issues

Use [GitHub Issues](https://github.com/lemonlion/TestTrackingDiagrams/issues) to report bugs or request features. Please include:

- .NET SDK version and target framework
- Test framework and version
- Minimal reproduction steps
- Expected vs actual behaviour

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
