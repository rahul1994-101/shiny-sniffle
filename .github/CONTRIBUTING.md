# 🤝 Contributing to TaskHub

Welcome! 👋 Thanks for your interest in contributing to **Shinny-Sniffle** — a full-stack learning project using .NET and Azure with GitHub best practices.

---

## 📦 Repo Structure

- `src/Frontend` → Blazor WebAssembly
- `src/Backend` → ASP.NET Core API
- `src/Database` → SQL Project (.dacpac)
- `.github/` → GitHub Actions, PR templates, CODEOWNERS

---

## 🚀 Getting Started

1. **Fork** the repo (if you're external)
2. **Clone** your fork
3. Create a new branch from `development`:
   ```bash
   git checkout development
   git pull
   git checkout -b feature/your-feature
   ```
4. Commit and push your changes:
   ```bash
   git commit -m "Add: [your feature summary]"
   git push origin feature/your-feature
   ```

---

## 🔁 Pull Requests

- Always target the `development` branch
- PRs must pass all CI checks
- Follow the [Pull Request Template](./PULL_REQUEST_TEMPLATE.md)
- Reviewers will be auto-assigned via [CODEOWNERS](./CODEOWNERS)

---

## 📜 Commit Message Format

Use clear, conventional commits:

- `Add:` for new features
- `Fix:` for bug fixes
- `Refactor:` for cleanup
- `Docs:` for documentation
- `Test:` for tests

Example:
```
Add: task reminder queue integration with Azure Function
```

---

## 🛡️ Guidelines

- Keep code modular and clean
- Avoid hard-coded secrets (use environment variables or GitHub Secrets)
- Use descriptive names for branches and commits

---

## 🙌 Thanks

We’re building this to learn and grow — your contribution helps make that happen! 💡
