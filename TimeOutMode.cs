namespace Eve.Caching
{
    public enum TimeOutMode : byte
    {
        LastUse = 0,
        FromCreate = 1,
        AccessCount = 2,
        Never = 3
    }
}
