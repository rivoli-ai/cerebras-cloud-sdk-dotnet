# API Documentation Guide

This guide explains how to generate and host API documentation for the Cerebras.Cloud.Sdk.

## Documentation Options

### 1. **GitHub Pages** (Recommended)
The project is configured to automatically generate and publish API documentation to GitHub Pages.

**Setup:**
1. Go to your repository Settings → Pages
2. Set Source to "GitHub Actions"
3. The documentation will be available at: `https://rivoli-ai.github.io/cerebras-cloud-sdk-dotnet/`

**Automatic Generation:**
- Documentation is automatically generated and published when you push to `main` or create a version tag
- Manual trigger available via Actions tab → "Generate API Documentation" → "Run workflow"

### 2. **Local Generation**
Generate documentation locally for preview:

```bash
# Generate documentation
./scripts/generate-docs.sh

# Preview locally
docfx docfx.json --serve

# Open http://localhost:8080 in your browser
```

### 3. **NuGet Package Documentation**
The package on NuGet.org automatically includes:
- XML documentation comments from your code
- README.md displayed on the package page
- Source Link for F12 navigation in Visual Studio

### 4. **Alternative Documentation Generators**

#### **Sandcastle Help File Builder**
Traditional Windows Help format documentation:
```xml
<!-- In your .csproj -->
<PackageReference Include="EWSoftware.SHFB" Version="*" />
```

#### **Swagger/OpenAPI** (for Web APIs)
If you create a Web API wrapper:
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

#### **Docusaurus**
Modern documentation site generator:
```bash
npx create-docusaurus@latest docs classic
```

## XML Documentation Comments

Ensure your code has comprehensive XML comments:

```csharp
/// <summary>
/// Creates a chat completion request.
/// </summary>
/// <param name="request">The chat completion request parameters.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The chat completion response.</returns>
/// <exception cref="CerebrasApiException">Thrown when the API returns an error.</exception>
/// <example>
/// <code>
/// var response = await client.Chat.CreateAsync(new ChatCompletionRequest
/// {
///     Model = "llama-3.3-70b",
///     Messages = new List&lt;ChatMessage&gt;
///     {
///         new() { Role = "user", Content = "Hello!" }
///     }
/// });
/// </code>
/// </example>
public async Task<ChatCompletionResponse> CreateAsync(
    ChatCompletionRequest request,
    CancellationToken cancellationToken = default)
```

## Documentation Best Practices

1. **Use XML Comments Extensively**
   - Document all public APIs
   - Include examples in `<example>` tags
   - Document exceptions with `<exception>` tags
   - Use `<see>` and `<seealso>` for cross-references

2. **Keep Documentation Updated**
   - Update docs when API changes
   - Review generated documentation regularly
   - Include migration guides for breaking changes

3. **Organize Content**
   - Group related APIs together
   - Provide getting started guides
   - Include code samples
   - Add troubleshooting sections

4. **Version Your Documentation**
   - Tag documentation with release versions
   - Maintain documentation for multiple versions
   - Clearly mark deprecated features

## Publishing Documentation

### GitHub Pages (Automatic)
Documentation is automatically published via GitHub Actions when:
- Pushing to `main` branch
- Creating version tags (`v*`)

### Manual Publishing Options

1. **Azure Static Web Apps**
   ```yaml
   - name: Deploy to Azure Static Web Apps
     uses: Azure/static-web-apps-deploy@v1
     with:
       azure_static_web_apps_api_token: ${{ secrets.AZURE_TOKEN }}
       repo_token: ${{ secrets.GITHUB_TOKEN }}
       action: "upload"
       app_location: "_site"
   ```

2. **Netlify**
   ```bash
   # Install Netlify CLI
   npm install -g netlify-cli
   
   # Deploy
   netlify deploy --dir=_site --prod
   ```

3. **Read the Docs**
   - Connect your GitHub repository
   - Configure `.readthedocs.yaml`
   - Automatic builds on push

## Adding Custom Documentation

1. Create markdown files in the `articles` folder
2. Update `toc.yml` to include new sections
3. Add images to the `images` folder
4. Reference them in your markdown:
   ```markdown
   ![Architecture Diagram](images/architecture.png)
   ```

## Monitoring Documentation

- Check broken links regularly
- Monitor 404s in GitHub Pages insights
- Review search analytics
- Gather user feedback

## Integration with NuGet Package

The package already includes:
- XML documentation file (`Cerebras.Cloud.Sdk.xml`)
- Source Link for debugging
- Symbol package (`.snupkg`)

To ensure documentation appears in IntelliSense:
1. XML comments are included via `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
2. Documentation file is packaged automatically
3. IDEs will show tooltips from your XML comments

## Next Steps

1. Enable GitHub Pages in repository settings
2. Run the documentation workflow
3. Access your docs at: https://rivoli-ai.github.io/cerebras-cloud-sdk-dotnet/
4. Add the documentation URL to your NuGet package metadata and README