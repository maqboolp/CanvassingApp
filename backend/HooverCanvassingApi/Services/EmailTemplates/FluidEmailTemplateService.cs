using Fluid;
using HooverCanvassingApi.Configuration;
using Microsoft.Extensions.Options;

namespace HooverCanvassingApi.Services.EmailTemplates
{
    public interface IEmailTemplateService
    {
        Task<string> RenderTemplateAsync(string templateName, object model);
        Task<(string subject, string htmlBody, string textBody)> RenderEmailAsync(string templateName, object model);
    }

    public class FluidEmailTemplateService : IEmailTemplateService
    {
        private readonly CampaignSettings _campaignSettings;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FluidEmailTemplateService> _logger;
        private readonly FluidParser _parser;
        private readonly TemplateOptions _templateOptions;
        private readonly string _templatesPath;

        public FluidEmailTemplateService(
            IOptions<CampaignSettings> campaignSettings,
            IWebHostEnvironment environment,
            ILogger<FluidEmailTemplateService> logger)
        {
            _campaignSettings = campaignSettings.Value;
            _environment = environment;
            _logger = logger;
            _parser = new FluidParser();
            _templateOptions = new TemplateOptions();
            _templatesPath = Path.Combine(_environment.ContentRootPath, "EmailTemplates");

            // Register custom filters if needed
            _templateOptions.Filters.AddFilter("date", (input, arguments, context) =>
            {
                if (input is DateTime dateTime)
                {
                    var format = arguments.At(0).ToStringValue() ?? "MM/dd/yyyy";
                    return new StringValue(dateTime.ToString(format));
                }
                return NilValue.Instance;
            });
        }

        public async Task<string> RenderTemplateAsync(string templateName, object model)
        {
            try
            {
                var templatePath = Path.Combine(_templatesPath, $"{templateName}.liquid");
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Email template '{templateName}' not found at {templatePath}");
                }

                var templateContent = await File.ReadAllTextAsync(templatePath);
                
                if (_parser.TryParse(templateContent, out var template, out var error))
                {
                    var context = new TemplateContext(model, _templateOptions);
                    
                    // Add campaign settings to context
                    context.SetValue("Campaign", _campaignSettings);
                    context.SetValue("CurrentYear", DateTime.UtcNow.Year);
                    
                    return await template.RenderAsync(context);
                }
                else
                {
                    _logger.LogError("Failed to parse template {TemplateName}: {Error}", templateName, error);
                    throw new InvalidOperationException($"Failed to parse template: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering template {TemplateName}", templateName);
                throw;
            }
        }

        public async Task<(string subject, string htmlBody, string textBody)> RenderEmailAsync(string templateName, object model)
        {
            // Render the main HTML template
            var htmlBody = await RenderTemplateAsync(templateName, model);
            
            // Try to render subject template
            string subject = "";
            var subjectTemplatePath = Path.Combine(_templatesPath, $"{templateName}.subject.liquid");
            if (File.Exists(subjectTemplatePath))
            {
                var subjectTemplate = await File.ReadAllTextAsync(subjectTemplatePath);
                if (_parser.TryParse(subjectTemplate, out var template, out var error))
                {
                    var context = new TemplateContext(model, _templateOptions);
                    context.SetValue("Campaign", _campaignSettings);
                    subject = await template.RenderAsync(context);
                }
            }
            
            // Try to render text version
            string textBody = "";
            var textTemplatePath = Path.Combine(_templatesPath, $"{templateName}.text.liquid");
            if (File.Exists(textTemplatePath))
            {
                textBody = await RenderTemplateAsync($"{templateName}.text", model);
            }
            else
            {
                // Simple HTML to text conversion
                textBody = System.Text.RegularExpressions.Regex.Replace(htmlBody, "<[^>]*>", "");
            }
            
            return (subject.Trim(), htmlBody, textBody);
        }
    }
}