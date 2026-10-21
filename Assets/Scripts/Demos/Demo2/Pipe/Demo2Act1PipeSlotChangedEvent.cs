namespace Spotlight.Demos.Demo2
{
    /// <summary>
    /// 描述第一幕稳定管道Slot的占用或通水状态已经改变的事实。
    /// </summary>
    public readonly struct Demo2Act1PipeSlotChangedEvent
    {
        public Demo2Act1PipeSlotChangedEvent(string slotId, bool isOccupied, bool isWet)
        {
            SlotId = slotId;
            IsOccupied = isOccupied;
            IsWet = isWet;
        }

        public string SlotId { get; }

        public bool IsOccupied { get; }

        public bool IsWet { get; }
    }
}
