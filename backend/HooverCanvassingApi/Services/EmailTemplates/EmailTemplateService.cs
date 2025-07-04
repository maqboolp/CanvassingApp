using HooverCanvassingApi.Configuration;
using Microsoft.Extensions.Options;

namespace HooverCanvassingApi.Services.EmailTemplates
{
    public interface IEmailTemplateService
    {
        string GetTemplate(string templateName);
        string RenderTemplate(string templateName, object model);
    }

    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly CampaignSettings _campaignSettings;
        private readonly string _templatesPath;

        public EmailTemplateService(IOptions<CampaignSettings> campaignSettings, IWebHostEnvironment env)
        {
            _campaignSettings = campaignSettings.Value;
            _templatesPath = Path.Combine(env.ContentRootPath, "EmailTemplates");
        }

        public string GetTemplate(string templateName)
        {
            var templatePath = Path.Combine(_templatesPath, $"{templateName}.html");
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Email template '{templateName}' not found.");
            }
            return File.ReadAllText(templatePath);
        }

        public string RenderTemplate(string templateName, object model)
        {
            var template = GetTemplate(templateName);
            
            // Replace campaign settings placeholders
            template = template.Replace("{{CampaignName}}", _campaignSettings.CampaignName);
            template = template.Replace("{{CandidateName}}", _campaignSettings.CandidateName);
            template = template.Replace("{{CampaignTitle}}", _campaignSettings.CampaignTitle);
            template = template.Replace("{{PaidForBy}}", _campaignSettings.PaidForBy);
            template = template.Replace("{{CampaignEmail}}", _campaignSettings.CampaignEmail);
            template = template.Replace("{{CampaignPhone}}", _campaignSettings.CampaignPhone);
            template = template.Replace("{{CampaignWebsite}}", _campaignSettings.CampaignWebsite);
            template = template.Replace("{{CampaignAddress}}", _campaignSettings.CampaignAddress);
            template = template.Replace("{{Office}}", _campaignSettings.Office);
            template = template.Replace("{{Jurisdiction}}", _campaignSettings.Jurisdiction);
            
            // Replace model properties using reflection
            if (model != null)
            {
                var properties = model.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(model)?.ToString() ?? string.Empty;
                    template = template.Replace($"{{{{{prop.Name}}}}}", value);
                }
            }
            
            return template;
        }
    }
}