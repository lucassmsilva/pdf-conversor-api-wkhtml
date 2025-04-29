using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using WkHtmlToPdfDotNet.Contracts;
using WkHtmlToPdfDotNet;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var converter = new WkHtmlToPdfDotNet.SynchronizedConverter(new WkHtmlToPdfDotNet.PdfTools());
builder.Services.AddSingleton<WkHtmlToPdfDotNet.Contracts.IConverter>(converter);

// Adiciona serviços ao contêiner.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

var app = builder.Build();

// Configura o pipeline de requisições HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/generate-pdf-from-body", async (HttpRequest request, IConverter converter) =>
{
    string htmlContent;
    using (var reader = new StreamReader(request.Body))
    {
        htmlContent = await reader.ReadToEndAsync();
    }

    if (string.IsNullOrWhiteSpace(htmlContent))
    {
        return Results.BadRequest("HTML content cannot be empty.");
    }

    try
    {
        Console.WriteLine("Starting PDF generation from body...");

        var doc = new HtmlToPdfDocument()
        {
            GlobalSettings = {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
            Objects = {
                new ObjectSettings() {
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
        };

        Console.WriteLine("Converting HTML to PDF...");
        var pdfBytes = converter.Convert(doc);
        Console.WriteLine("PDF generated successfully from body.");

        return Results.File(pdfBytes, "application/pdf", "document_from_body.pdf");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error generating PDF from body: {ex.Message}\n{ex.StackTrace}");
        return Results.Problem($"Internal error generating PDF: {ex.Message}");
    }
})
.WithName("GeneratePdfFromBody")
.WithDescription("Receives raw HTML in the body. CSS/Images should use absolute URLs or be embedded. Returns a PDF.")
.Accepts<string>("text/html")
.Produces<FileContentResult>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError);



app.MapPost("/generate-pdf-from-url", async (GenerateUrlRequest request, IConverter converter) =>
{
    if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
    {
        return Results.BadRequest("URL provided is invalid.");
    }

    try
    {
        Console.WriteLine($"Starting PDF generation from URL: {uri}");

        var doc = new HtmlToPdfDocument()
        {
            GlobalSettings = {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
            Objects = {
                new ObjectSettings() {
                    Page = uri.ToString(),
                    WebSettings = {
                        DefaultEncoding = "utf-8",
                        LoadImages = true,
                        EnableJavascript = true,
                        EnableIntelligentShrinking = true
                    }
                }
            }
        };

        Console.WriteLine("Converting URL to PDF...");
        var pdfBytes = converter.Convert(doc);
        Console.WriteLine("PDF generated successfully from URL.");

        return Results.File(pdfBytes, "application/pdf", "document_from_url.pdf");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error generating PDF from URL: {ex.Message}\n{ex.StackTrace}");
        return Results.Problem($"Internal error generating PDF: {ex.Message}");
    }
})
.WithName("GeneratePdfFromUrl")
.WithDescription("Receives a JSON { \"url\": \"your-url\" }, navigates to the URL and returns a PDF.")
.Accepts<GenerateUrlRequest>("application/json")
.Produces<FileContentResult>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError);


app.MapPost("/generate-pdf-from-file", async (IFormFile file, IConverter converter) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest("No file uploaded or empty file.");
    }

    string htmlContent;
    try
    {
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            htmlContent = await reader.ReadToEndAsync();
        }
        Console.WriteLine($"File '{file.FileName}' read successfully ({file.Length} bytes).");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error reading uploaded file '{file.FileName}': {ex.Message}");
        return Results.Problem($"Error reading uploaded file: {ex.Message}");
    }

    if (string.IsNullOrWhiteSpace(htmlContent))
    {
        return Results.BadRequest("HTML content in file is empty.");
    }

    try
    {
        Console.WriteLine($"Starting PDF generation from file '{file.FileName}'...");

        var doc = new HtmlToPdfDocument()
        {
            GlobalSettings = {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
            Objects = {
                new ObjectSettings() {
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
        };

        Console.WriteLine("Converting file content to PDF...");
        var pdfBytes = converter.Convert(doc);
        Console.WriteLine($"PDF generated successfully from file '{file.FileName}'.");

        var outputFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}.pdf";
        return Results.File(pdfBytes, "application/pdf", outputFileName);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error generating PDF from file: {ex.Message}\n{ex.StackTrace}");
        return Results.Problem($"Internal error generating PDF: {ex.Message}");
    }
})
.WithName("GeneratePdfFromFile")
.WithDescription("Receives an HTML file upload. CSS/Images should use absolute URLs or be embedded. Returns a PDF.")
.Accepts<IFormFile>("multipart/form-data")
.Produces<FileContentResult>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError)
.DisableAntiforgery();

// Mapeamento de rota raiz para teste simples
// Rota raiz para teste
app.MapGet("/", () => "API de Geração de PDF (v2) está online! Endpoints: POST /generate-pdf-from-body, POST /generate-pdf-from-url, POST /generate-pdf-from-file");


app.Use(async (context, next) =>
{
    // Verifica se o caminho da requisição é exatamente a raiz "/"
    if (context.Request.Path == "/")
    {
        // Define a resposta como um redirecionamento para a URL do Swagger UI
        // A URL base é inferida, só precisamos do caminho relativo.
        context.Response.Redirect("/swagger/index.html"); // Caminho padrão do Swagger UI
        return; // Importante: Finaliza o processamento desta requisição aqui
    }

    // Se não for a raiz, continua para o próximo middleware ou endpoint
    await next(context);
});

app.Run();


public record GenerateUrlRequest(string Url);

// O contexto para geração de código fonte do JSON
[JsonSerializable(typeof(GenerateUrlRequest))]
// Adicione outros tipos aqui se necessário
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}