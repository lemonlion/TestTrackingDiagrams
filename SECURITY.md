# Security Policy

## Supported Versions

| Version | Supported          |
|---------|--------------------|
| 2.0.x   | :white_check_mark: |
| 1.x     | :x:                |

## Reporting a Vulnerability

If you discover a security vulnerability in TestTrackingDiagrams, please report it responsibly.

**Do not open a public GitHub issue for security vulnerabilities.**

Instead, please email **[the repository owner](https://github.com/lemonlion)** directly, or use [GitHub's private vulnerability reporting](https://github.com/lemonlion/TestTrackingDiagrams/security/advisories/new).

Include:

- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

You can expect an initial response within 7 days. We will work with you to understand the issue and coordinate a fix before any public disclosure.

## Scope

TestTrackingDiagrams is a **test-time library** — it intercepts HTTP traffic during test execution and generates reports. It is not intended for production runtime use. Security considerations focus on:

- Safe handling of test data in generated reports
- No injection of untrusted content into HTML output
- Safe file I/O for report generation
