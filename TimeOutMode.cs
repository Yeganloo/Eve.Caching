namespace Eve.Caching
{
    public enum TimeOutMode : byte
    {
        LastUse = 1,
        FromCreate = 2,
        AccessCount = 4,
        Never = 8
    }
}
