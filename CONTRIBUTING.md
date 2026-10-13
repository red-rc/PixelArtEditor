# Contributing to Pixeller

Thank you for your interest in contributing to **Pixeller**! We welcome bug reports, feature suggestions, and code contributions to help make this project better for everyone.

---

## How Can I Contribute?

### 1. Reporting Bugs

Before creating a new issue, please check existing issues to avoid duplicates. When reporting a bug, please include:

- A clear and descriptive title
- Steps to reproduce the issue
- Expected vs. actual behavior
- Information about your environment (OS version, .NET runtime version)

### 2. Suggesting Enhancements

Feature requests are welcome! Please clearly explain:

- The problem or use case you are trying to solve
- How the proposed feature should work

### 3. Submitting Code (Pull Requests)

If you'd like to fix a bug or add a new feature:

1. **Fork the Repository:** Create your own fork of Pixeller on GitHub.
2. **Clone Locally:**

   ```bash
   git clone https://github.com/red-rc/pixeller.git
   cd pixeller
   ```

3. **Create a Feature Branch:**

   For new features:

   ```bash
   git checkout -b feature/your-feature-name
   ```

   For bug fixes:

   ```bash
   git checkout -b fix/your-bug-fix
   ```

4. **Make Your Changes:** Follow the project's coding standards and C#/.NET conventions.
5. **Test Your Changes:** Ensure the project builds successfully with `dotnet build` and runs as expected on your target OS.
6. **Commit and Push:**

   ```bash
   git add .
   git commit -m "Add descriptive summary of changes"
   git push origin feature/your-feature-name
   ```

7. **Open a Pull Request:** Navigate to the original repository and submit a pull request targeting the `master` branch.

## Technical Environment

- **Language:** C#
- **UI Framework:** [Avalonia UI](https://avaloniaui.net/)
- **SDK:** .NET 10.0

## Code of Conduct

Please refer to the project's [Code of Conduct](CODE_OF_CONDUCT.md). By participating in this project, you agree to abide by its terms.
