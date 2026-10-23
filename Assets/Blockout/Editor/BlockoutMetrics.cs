namespace Blockout
{
    /// <summary>
    /// Every size in the kit comes from here, so the whole kit can be re-scaled in one place.
    ///
    /// Sources:
    ///  - The Level Design Book, "Metrics" (book.leveldesignbook.com/process/blockout/metrics), Unity row:
    ///      player bounding box 1.0 x 1.8 m, eye height 1.5-1.7 m, interior wall 0.1 x 3.0 m,
    ///      minimum hall width 2.0 m, door 1.25 x 2.5 m.
    ///  - The Level Design Book, "Blockout": grid based on player width (Unity: 1 m player -> grid 1 or 2),
    ///      walls ~150-200% of the scale figure's height, entryways/hallways at least 2x the figure's width,
    ///      window = doorway-like gap filled with a waist-height wall.
    ///  - The Level Design Book, "Modular kit design": floors are NOT paper thin (stack test),
    ///      "glue" pieces fill joints, every module stays inside a footprint on the grid.
    /// </summary>
    public static class BlockoutMetrics
    {
        // Player (physics metrics - measured)
        public const float PlayerWidth = 1.0f;
        public const float PlayerHeight = 1.8f;
        public const float EyeStanding = 1.6f;
        public const float EyeSeated = 1.25f;    // our game is played sitting at a desk

        // Building metrics (our decisions)
        public const float Module = 2.0f;         // wall / floor module footprint (grid snap 1 or 2)
        public const float WallHeight = 3.0f;     // ~167% of the 1.8 m scale figure
        public const float WallThickness = 0.2f;  // book lists 0.1 m; 0.2 reads heavier and collides more reliably
        public const float FloorThickness = 0.2f;
        public const float DoorWidth = 1.25f;
        public const float DoorHeight = 2.5f;
        public const float WindowSill = 1.0f;     // waist height
        public const float WindowTop = 2.2f;
        public const float StepHeight = 0.15f;    // teacher's podium = one step
        public const float MinHallWidth = 2.0f;   // 2x player width; corridor is built at 4 m
    }
}
