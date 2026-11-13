// Startup check of the four providers' settings (ValidateOnStart). The rule that bites in practice:
// a BaseAddress without a trailing slash makes relative request paths silently replace its last segment.
using Microsoft.Extensions.Options;
using Tollgate.Providers.Rdap;
using Tollgate.Providers.SafeBrowsing;
using Tollgate.Providers.UrlHaus;
using Tollgate.Providers.VirusTotal;

namespace Tollgate.Api.Configuration;

public sealed class ProviderOptionsValidator :
    IValidateOptions<VirusTotalOptions>,
    IValidateOptions<SafeBrowsingOptions>,
    IValidateOptions<UrlHausOptions>,
    IValidateOptions<RdapOptions>
{
    public ValidateOptionsResult Validate(string? name, VirusTotalOptions options)
    {
        var errors = BaseAddressErrors(VirusTotalOptions.SectionName, options.BaseAddress);

        if (options.AnalysisPollAttempts < 1)
        {
            errors.Add($"{VirusTotalOptions.SectionName}:AnalysisPollAttempts must be at least 1");
        }

        if (options.AnalysisPollDelay < TimeSpan.Zero)
        {
            errors.Add($"{VirusTotalOptions.SectionName}:AnalysisPollDelay must not be negative");
        }

        return ToResult(errors);
    }

    public ValidateOptionsResult Validate(string? name, SafeBrowsingOptions options) =>
        ToResult(BaseAddressErrors(SafeBrowsingOptions.SectionName, options.BaseAddress));

    public ValidateOptionsResult Validate(string? name, UrlHausOptions options) =>
        ToResult(BaseAddressErrors(UrlHausOptions.SectionName, options.BaseAddress));

    public ValidateOptionsResult Validate(string? name, RdapOptions options) =>
        ToResult(BaseAddressErrors(RdapOptions.SectionName, options.BaseAddress));

    private static List<string> BaseAddressErrors(string section, Uri? baseAddress)
    {
        if (baseAddress is null)
        {
            return [$"{section}:BaseAddress is required"];
        }

        var errors = new List<string>();
        if (baseAddress.Scheme != Uri.UriSchemeHttps && baseAddress.Scheme != Uri.UriSchemeHttp)
        {
            errors.Add($"{section}:BaseAddress must be an absolute http(s) address");
        }

        if (!baseAddress.AbsolutePath.EndsWith('/'))
        {
            errors.Add($"{section}:BaseAddress must end with a slash");
        }

        return errors;
    }

    private static ValidateOptionsResult ToResult(List<string> errors) =>
        errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
}
