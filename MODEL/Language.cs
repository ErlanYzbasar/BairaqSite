namespace MODEL;

public class Language
{
    public byte BackendDisplay { get; set; }
    public byte DisplayOrder { get; set; }
    public byte FrontendDisplay { get; set; }
    public string FullName { get; set; }
    public uint Id { get; set; }
    public byte IsDefault { get; set; }
    public string IsoCode { get; set; }
    public byte IsSubLanguage { get; set; }
    public string LanguageCulture { get; set; }
    public string LanguageFlagImageUrl { get; set; }
    public byte QStatus { get; set; }
    public string ShortName { get; set; }
    public string UniqueSeoCode { get; set; }
}