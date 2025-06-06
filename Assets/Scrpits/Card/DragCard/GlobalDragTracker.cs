public static class GlobalDragTracker
{
    public static bool IsDraggingCard { get; private set; } = false;

    public static void BeginDrag()
    {
        IsDraggingCard = true;
    }

    public static void EndDrag()
    {
        IsDraggingCard = false;
    }
}
