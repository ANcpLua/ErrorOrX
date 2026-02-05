# ErrorOrX Railway Deployment

## Railway Environment Variables (Template)

When deployed, Railway will provide these system variables:

| Variable | Description | Example |
|----------|-------------|---------|
| `RAILWAY_PUBLIC_DOMAIN` | Public service domain | `errororx-production.up.railway.app` |
| `RAILWAY_PRIVATE_DOMAIN` | Internal DNS name | `errororx.railway.internal` |
| `RAILWAY_PROJECT_NAME` | Project name | `errororx` |
| `RAILWAY_ENVIRONMENT_NAME` | Environment | `production` |
| `RAILWAY_SERVICE_NAME` | Service name | `errororx-api` |
| `RAILWAY_PROJECT_ID` | Project UUID | `(auto-generated)` |
| `RAILWAY_ENVIRONMENT_ID` | Environment UUID | `(auto-generated)` |
| `RAILWAY_SERVICE_ID` | Service UUID | `(auto-generated)` |

## Deployment Checklist

- [ ] Create Railway project
- [ ] Configure service
- [ ] Set custom environment variables (if needed)
- [ ] Configure domain
- [ ] Deploy

## Reference: qyl-api (sister project)

| Variable | Value |
|----------|-------|
| `RAILWAY_PUBLIC_DOMAIN` | `qyl-api-production.up.railway.app` |
| `RAILWAY_PROJECT_ID` | `5eaa4020-71d9-4828-89d3-316cb188529e` |
| `RAILWAY_ENVIRONMENT_ID` | `616ff7bf-ef19-4e34-bb22-d3eb002b74e9` |
| `RAILWAY_SERVICE_ID` | `ff836187-b65c-4645-ab40-67b54fbfa93f` |
