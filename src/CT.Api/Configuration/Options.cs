namespace CT.Api.Configuration;

public class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "CT.Api";
    public string Audience { get; set; } = "CT.Clients";
    public string Key { get; set; } = "";
    public int DureeHeures { get; set; } = 8;
}
