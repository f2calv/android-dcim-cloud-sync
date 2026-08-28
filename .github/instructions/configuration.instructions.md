---
description: 'Mobile configuration rules for public defaults, validation and secret storage.'
applyTo: '**/appsettings*.json,**/*Config.cs'
---

# Configuration

## Public Configuration

- Treat every tracked configuration file and every resource packaged into the application as public.
- Store only non-sensitive defaults in tracked configuration: retry limits, concurrency, network policy and feature flags.
- Use generic placeholders in documentation and development examples.
- Keep configuration models synchronized with every tracked configuration example in the same change.
- Validate configuration before backup work starts and return actionable errors without echoing sensitive values.

## Secrets

- Never place an Azure Storage account key, connection string, client secret, signing password, long-lived SAS, private endpoint or personal identifier in tracked configuration.
- Local developer configuration files must use the existing `appsettings.Local*.json` ignore pattern and must never be embedded in an application package.
- Runtime secrets entered by a user belong in .NET MAUI `SecureStorage`, not `Preferences`, SQLite, logs or telemetry.
- Prefer short-lived, least-privilege authorization and store an expiry time so the application can request renewal safely.
