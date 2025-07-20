namespace TestApp.CyberArk.Options
{
    public class SamlOptions
    {
        public string EntityId { get; set; } = string.Empty;
        public string MetadataUrl { get; set; } = string.Empty;
        public string CallbackPath { get; set; } = "/saml2/acs";
        public string LogoutPath { get; set; } = "/saml2/logout";
    }
}