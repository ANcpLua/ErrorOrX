### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
EOE017 | ErrorOr.Endpoints | Error | Anonymous return type not supported
EOE018 | ErrorOr.Endpoints | Error | Nested [AsParameters] not supported
EOE019 | ErrorOr.Endpoints | Error | Nullable [AsParameters] not supported
EOE020 | ErrorOr.Endpoints | Error | Inaccessible type in endpoint

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|------
EOE025 | ErrorOr.Endpoints | Warning | ErrorOr.Endpoints | Error | Downgraded to warning for bodyless/custom method inference
