# Release Process

## Overview

This project uses GitHub Actions for automated builds, testing, and publishing to NuGet.org. The release process supports both pre-releases and stable releases.

## Versioning Strategy

- **Stable Releases**: Tagged versions (e.g., `v1.0.0`)
- **Release Candidates**: Commits to `main` branch (e.g., `1.0.0-rc.123`)
- **Beta Releases**: Commits to `develop` branch (e.g., `1.0.0-beta.456`)

## Pre-Release Process

### Beta Releases (develop branch)
1. Make changes in feature branches
2. Create PR to `develop` branch
3. Upon merge, GitHub Actions automatically:
   - Builds the solution
   - Runs all tests
   - Creates a beta package (e.g., `1.0.0-beta.123`)
   - Publishes to NuGet.org

### Release Candidates (main branch)
1. Create PR from `develop` to `main`
2. Upon merge, GitHub Actions automatically:
   - Builds the solution
   - Runs all tests
   - Creates an RC package (e.g., `1.0.0-rc.456`)
   - Publishes to NuGet.org

## Stable Release Process

1. **Prepare Release**
   - Ensure all tests pass on `main`
   - Update version in `Directory.Build.props`
   - Update CHANGELOG.md

2. **Create Release**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. **Automated Actions**
   - GitHub Actions detects the tag
   - Builds and tests the solution
   - Creates a stable package (e.g., `1.0.0`)
   - Publishes to NuGet.org
   - Creates a GitHub Release with release notes

## Setup Requirements

### GitHub Secrets
Configure these secrets in your GitHub repository:
- `NUGET_API_KEY`: Your NuGet.org API key

### Branch Protection
Recommended settings:
- Protect `main` and `develop` branches
- Require PR reviews
- Require status checks to pass
- Run tests on all PRs

## Manual Release (if needed)

```bash
# Build
dotnet build --configuration Release

# Test
dotnet test --configuration Release

# Pack
dotnet pack src/Cerebras.Cloud.Sdk/Cerebras.Cloud.Sdk.csproj \
  --configuration Release \
  -p:PackageVersion=1.0.0

# Publish
dotnet nuget push ./artifacts/*.nupkg \
  --source https://api.nuget.org/v3/index.json \
  --api-key YOUR_API_KEY
```

## Version Numbering

Follow Semantic Versioning:
- MAJOR: Breaking changes
- MINOR: New features (backwards compatible)
- PATCH: Bug fixes (backwards compatible)

Pre-release versions:
- Beta: Early development, may have breaking changes
- RC: Feature complete, final testing phase