namespace Edgar.Service;

public class WebSocketSettings
{
    public int KeepAliveIntervalSeconds { get; set; } = 120;
    public int KeepAliveTimeoutSeconds { get; set; } = 120;
    public string[] AllowedOrigins { get; set; } = ["*"];
}