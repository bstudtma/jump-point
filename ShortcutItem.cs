namespace JumpPoint
{
    public class ShortcutItem
    {
        public string DisplayName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;

        public string Arguments { get; set; } = string.Empty;

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
