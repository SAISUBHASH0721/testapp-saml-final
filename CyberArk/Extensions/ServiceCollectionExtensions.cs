using Microsoft.AspNetCore.Authentication.Cookies;
using TestApp.CyberArk.Options;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;

namespace TestApp.CyberArk.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSamlAuthentication(
            this IServiceCollection services,
            Action<SamlOptions> configureOptions)
        {
            var samlOptions = new SamlOptions();
            configureOptions(samlOptions);

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Saml2Defaults.Scheme;
            })
            .AddCookie()
            .AddSaml2(Saml2Defaults.Scheme, options =>
            {
                options.SPOptions.EntityId = new EntityId(samlOptions.EntityId);

                // Extract CyberArk entity ID from metadata URL
                var cyberArkEntityId = ExtractCyberArkEntityId(samlOptions.MetadataUrl);

                var idp = new IdentityProvider(new EntityId(cyberArkEntityId), options.SPOptions)
                {
                    MetadataLocation = samlOptions.MetadataUrl,
                    LoadMetadata = true,
                    AllowUnsolicitedAuthnResponse = true,
                    WantAuthnRequestsSigned = false,
                    DisableOutboundLogoutRequests = false
                };

                options.IdentityProviders.Add(idp);

                // Configure module path
                if (!string.IsNullOrEmpty(samlOptions.CallbackPath))
                {
                    var modulePath = samlOptions.CallbackPath.Replace("/acs", "");
                    options.SPOptions.ModulePath = modulePath;
                }

                // Fix for IDP-initiated login (CyberArk portal clicks)
                options.SPOptions.ReturnUrl = new Uri(samlOptions.EntityId);
            });

            return services;
        }

        private static string ExtractCyberArkEntityId(string metadataUrl)
        {
            try
            {
                var uri = new Uri(metadataUrl);
                var query = uri.Query;
                if (!string.IsNullOrEmpty(query))
                {
                    var parameters = query.TrimStart('?').Split('&');
                    foreach (var param in parameters)
                    {
                        var parts = param.Split('=');
                        if (parts.Length == 2 && parts[0].Equals("appkey", StringComparison.OrdinalIgnoreCase))
                        {
                            var appKey = Uri.UnescapeDataString(parts[1]);
                            return $"{uri.Scheme}://{uri.Host}/{appKey}";
                        }
                    }
                }
                return $"{uri.Scheme}://{uri.Host}/";
            }
            catch
            {
                var uri = new Uri(metadataUrl);
                return $"{uri.Scheme}://{uri.Host}/";
            }
        }
    }
}