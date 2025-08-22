using System.Text;

namespace HooverCanvassingApi.Services.EmailTemplates;

public class CampaignEmailTemplate
{
    public static (string subject, string htmlContent, string plainTextContent) GenerateCampaignEmail(
        CampaignEmailModel model, 
        string unsubscribeUrl,
        string baseUrl)
    {
        var subject = model.Subject;
        
        var htmlContent = GenerateHtmlContent(model, unsubscribeUrl, baseUrl);
        var plainTextContent = GeneratePlainTextContent(model, unsubscribeUrl);
        
        return (subject, htmlContent, plainTextContent);
    }
    
    private static string GenerateHtmlContent(CampaignEmailModel model, string unsubscribeUrl, string baseUrl)
    {
        var sb = new StringBuilder();
        
        sb.Append(@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>").Append(model.Subject).Append(@"</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            margin: 0;
            padding: 0;
            background-color: #f5f5f5;
            line-height: 1.6;
        }
        .container {
            max-width: 600px;
            margin: 0 auto;
            background-color: white;
        }
        .content {
            padding: 40px 30px;
            color: #333;
        }
        .content h2 {
            color: #673ab7;
            font-size: 24px;
            margin-top: 0;
            margin-bottom: 20px;
        }
        .content h3 {
            color: #512da8;
            font-size: 20px;
            margin-top: 30px;
            margin-bottom: 15px;
        }
        .content p {
            margin: 15px 0;
            line-height: 1.7;
        }
        .content ul {
            padding-left: 25px;
            margin: 15px 0;
        }
        .content li {
            margin: 10px 0;
        }
        .cta-button {
            display: inline-block;
            padding: 15px 35px;
            background: linear-gradient(135deg, #673ab7 0%, #512da8 100%);
            color: white !important;
            text-decoration: none;
            border-radius: 50px;
            font-weight: 600;
            font-size: 16px;
            margin: 25px 0;
            box-shadow: 0 4px 15px rgba(103, 58, 183, 0.3);
            transition: all 0.3s ease;
        }
        .cta-button:hover {
            box-shadow: 0 6px 20px rgba(103, 58, 183, 0.4);
            transform: translateY(-2px);
        }
        .button-center {
            text-align: center;
            margin: 30px 0;
        }
        .highlight-box {
            background: linear-gradient(135deg, #f3e7ff 0%, #e8daff 100%);
            border-left: 4px solid #673ab7;
            padding: 20px;
            margin: 25px 0;
            border-radius: 8px;
        }
        .highlight-box h4 {
            margin-top: 0;
            color: #512da8;
        }
        .social-links {
            text-align: center;
            padding: 20px;
            background-color: #fafafa;
        }
        .social-links a {
            display: inline-block;
            margin: 0 10px;
            color: #673ab7;
            text-decoration: none;
            font-weight: 500;
        }
        .footer {
            background-color: #f5f5f5;
            padding: 30px;
            text-align: center;
            font-size: 14px;
            color: #666;
        }
        .footer a {
            color: #673ab7;
            text-decoration: none;
        }
        .footer a:hover {
            text-decoration: underline;
        }
        .unsubscribe {
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            font-size: 12px;
            color: #999;
        }
        .divider {
            height: 1px;
            background: linear-gradient(to right, transparent, #ddd, transparent);
            margin: 30px 0;
        }
        @media only screen and (max-width: 600px) {
            .content {
                padding: 30px 20px;
            }
            .cta-button {
                padding: 12px 25px;
                font-size: 15px;
            }
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='content'>");
        
        // Process placeholders in the HTML content
        var processedContent = ReplacePlaceholders(model.HtmlContent, model);
        sb.Append(processedContent);
        
        // Add call-to-action button if provided
        if (!string.IsNullOrEmpty(model.CallToActionUrl) && !string.IsNullOrEmpty(model.CallToActionText))
        {
            sb.Append(@"
            <div class='button-center'>
                <a href='").Append(model.CallToActionUrl).Append(@"' class='cta-button'>
                    ").Append(model.CallToActionText).Append(@"
                </a>
            </div>");
        }
        
        // Add important dates/deadlines if provided
        if (!string.IsNullOrEmpty(model.ImportantDates))
        {
            sb.Append(@"
            <div class='highlight-box'>
                <h4>Important Dates</h4>
                ").Append(model.ImportantDates).Append(@"
            </div>");
        }
        
        sb.Append(@"
        </div>");
        
        // Add social links if provided
        if (model.ShowSocialLinks)
        {
            sb.Append(@"
        <div class='social-links'>
            <strong>Connect with us:</strong><br>");
            
            if (!string.IsNullOrEmpty(model.FacebookUrl))
                sb.Append(@"<a href='").Append(model.FacebookUrl).Append(@"'>Facebook</a>");
            
            if (!string.IsNullOrEmpty(model.TwitterUrl))
                sb.Append(@"<a href='").Append(model.TwitterUrl).Append(@"'>Twitter</a>");
            
            if (!string.IsNullOrEmpty(model.InstagramUrl))
                sb.Append(@"<a href='").Append(model.InstagramUrl).Append(@"'>Instagram</a>");
            
            if (!string.IsNullOrEmpty(model.WebsiteUrl))
                sb.Append(@"<a href='").Append(model.WebsiteUrl).Append(@"'>Website</a>");
            
            sb.Append(@"
        </div>");
        }
        
        // Footer with unsubscribe link
        sb.Append(@"
        <div class='footer'>
            <p><strong>").Append(model.CampaignTitle).Append(@"</strong></p>");
        
        if (!string.IsNullOrEmpty(model.ElectionDate))
        {
            sb.Append(@"
            <p>").Append(model.ElectionDate).Append(@" Election</p>");
        }
        
        sb.Append(@"
            <p>").Append(model.PaidForBy).Append(@"</p>
            
            <div class='unsubscribe'>
                <p>
                    <a href='").Append(unsubscribeUrl).Append(@"'>Unsubscribe from these emails</a> | 
                    <a href='").Append(baseUrl).Append(@"/privacy-policy'>Privacy Policy</a> | 
                    <a href='").Append(baseUrl).Append(@"/terms'>Terms of Service</a>
                </p>
                <p>").Append(model.CampaignAddress ?? "").Append(@"</p>
            </div>
        </div>
    </div>
</body>
</html>");
        
        return sb.ToString();
    }
    
    private static string GeneratePlainTextContent(CampaignEmailModel model, string unsubscribeUrl)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine(model.CampaignName.ToUpper());
        sb.AppendLine(model.CampaignTitle);
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();
        
        // Process placeholders and strip HTML tags for plain text
        var processedContent = ReplacePlaceholders(model.HtmlContent, model);
        sb.AppendLine(StripHtmlTags(processedContent));
        sb.AppendLine();
        
        // Call to action
        if (!string.IsNullOrEmpty(model.CallToActionUrl) && !string.IsNullOrEmpty(model.CallToActionText))
        {
            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"{model.CallToActionText}:");
            sb.AppendLine(model.CallToActionUrl);
            sb.AppendLine(new string('-', 40));
            sb.AppendLine();
        }
        
        // Important dates
        if (!string.IsNullOrEmpty(model.ImportantDates))
        {
            sb.AppendLine("IMPORTANT DATES:");
            sb.AppendLine(StripHtmlTags(model.ImportantDates));
            sb.AppendLine();
        }
        
        // Social links
        if (model.ShowSocialLinks)
        {
            sb.AppendLine("Connect with us:");
            if (!string.IsNullOrEmpty(model.FacebookUrl))
                sb.AppendLine($"Facebook: {model.FacebookUrl}");
            if (!string.IsNullOrEmpty(model.TwitterUrl))
                sb.AppendLine($"Twitter: {model.TwitterUrl}");
            if (!string.IsNullOrEmpty(model.InstagramUrl))
                sb.AppendLine($"Instagram: {model.InstagramUrl}");
            if (!string.IsNullOrEmpty(model.WebsiteUrl))
                sb.AppendLine($"Website: {model.WebsiteUrl}");
            sb.AppendLine();
        }
        
        // Footer
        sb.AppendLine(new string('=', 60));
        sb.AppendLine(model.CampaignTitle);
        if (!string.IsNullOrEmpty(model.ElectionDate))
            sb.AppendLine($"{model.ElectionDate} Election");
        sb.AppendLine(model.PaidForBy);
        sb.AppendLine();
        
        // Unsubscribe
        sb.AppendLine("To unsubscribe from these emails, visit:");
        sb.AppendLine(unsubscribeUrl);
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(model.CampaignAddress))
            sb.AppendLine(model.CampaignAddress);
        
        return sb.ToString();
    }
    
    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;
        
        // Simple HTML tag removal (for production, consider using HtmlAgilityPack)
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
    }
    
    private static string ReplacePlaceholders(string content, CampaignEmailModel model)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;
            
        // Replace placeholders with actual values
        // Using double curly braces as placeholders: {{PlaceholderName}}
        var result = content
            .Replace("{{RecipientName}}", model.RecipientName ?? "")
            .Replace("{{RecipientEmail}}", model.RecipientEmail ?? "")
            .Replace("{{CampaignName}}", model.CampaignName ?? "")
            .Replace("{{CampaignTitle}}", model.CampaignTitle ?? "")
            .Replace("{{CandidateName}}", model.CampaignName?.Replace(" for Hoover", "") ?? "")
            .Replace("{{ElectionDate}}", model.ElectionDate ?? "")
            .Replace("{{PaidForBy}}", model.PaidForBy ?? "")
            .Replace("{{CampaignAddress}}", model.CampaignAddress ?? "")
            .Replace("{{Subject}}", model.Subject ?? "")
            .Replace("{{WebsiteUrl}}", model.WebsiteUrl ?? "")
            .Replace("{{FacebookUrl}}", model.FacebookUrl ?? "")
            .Replace("{{TwitterUrl}}", model.TwitterUrl ?? "")
            .Replace("{{InstagramUrl}}", model.InstagramUrl ?? "");
            
        return result;
    }
}

public class CampaignEmailModel
{
    public string Subject { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string CampaignTitle { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? CallToActionUrl { get; set; }
    public string? CallToActionText { get; set; }
    public string? ImportantDates { get; set; }
    public bool ShowSocialLinks { get; set; }
    public string? FacebookUrl { get; set; }
    public string? TwitterUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? ElectionDate { get; set; }
    public string PaidForBy { get; set; } = string.Empty;
    public string? CampaignAddress { get; set; }
}