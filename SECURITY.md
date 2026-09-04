# Security Policy

## Supported Versions

Android DCIM Cloud Sync is in pre-release development. Security fixes will be applied to the latest code on the default branch until versioned releases are available.

## Reporting a Vulnerability

Use GitHub's private vulnerability reporting for this repository. Do not open a public issue containing exploit details, credentials, SAS tokens, storage endpoints, personal filenames, or media.

If private vulnerability reporting is unavailable, contact the repository owner through their GitHub profile and request a private communication channel without including sensitive details in the initial message.

Reports should include:

- The affected version or commit
- Reproduction steps that use synthetic data
- The expected and observed behavior
- The potential impact
- Any suggested mitigation

## Credential Handling

- Never commit Azure Storage keys, connection strings, SAS tokens, signing credentials, or keystores.
- Never include long-lived storage credentials in an APK or Android App Bundle.
- Use least-privilege, short-lived access where possible.
- Store secrets on the device only through platform-backed secure storage.
- Revoke exposed credentials immediately, even if the commit containing them is later removed.
