# AxlProtocolMusic.WebApp.IntegrationTests

This project is reserved for integration tests that exercise real external-provider behavior against the web application, such as MongoDB-backed service tests via `Mongo2Go`.

The folder structure mirrors the unit test project:

- `Controllers`
- `Repositories`
- `Services`

These tests are intentionally kept out of the current GitHub Actions workflow so CI continues to run only the fast unit suite for now.
