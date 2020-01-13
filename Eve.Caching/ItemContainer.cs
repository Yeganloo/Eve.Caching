using System;

namespace Eve.Caching
{
    [Serializable]
    public class ItemContainer<TContent>
    {
        public DateTime CreationTime { get; set; }

        public int AccessCounter { get; set; }

        public TimeOutMode Mode { get; set; }

        public TContent Content { get; set; }
    }
}
