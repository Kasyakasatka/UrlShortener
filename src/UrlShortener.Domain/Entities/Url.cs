public class Url
{
    public string ShortCode { get; set; }
    public string OriginalUrl { get; set; }
    public DateTimeOffset CreationTimestamp { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public bool IsActive { get; set; }
    public string ExpirationBucket { get; set; }
    public Url() { }

    public Url(string shortCode, string originalUrl, DateTimeOffset? expirationDate = null)
    {
        ShortCode = shortCode;
        OriginalUrl = originalUrl;
        CreationTimestamp = DateTimeOffset.UtcNow;
        ExpirationDate = expirationDate;
        IsActive = true;
        ExpirationBucket = expirationDate.HasValue ? expirationDate.Value.ToString("yyyy-MM-dd") : "never_expires";
    }

    public void Update(string newOriginalUrl, DateTimeOffset? newExpirationDate)
    {
        OriginalUrl = newOriginalUrl;
        ExpirationDate = newExpirationDate;
        ExpirationBucket = newExpirationDate.HasValue ? newExpirationDate.Value.ToString("yyyy-MM-dd") : "never_expires";
    }

    public bool IsExpired()
    {
        return ExpirationDate.HasValue && ExpirationDate.Value <= DateTimeOffset.UtcNow;
    }
}
