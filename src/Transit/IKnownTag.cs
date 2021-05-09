namespace Sellars.Transit.Alpha
{
    public interface IKnownTag
    {
        /// <summary>
        /// The tag to use for all non-null objects,
        /// especially for writers that return the same tag for every non-null value.
        /// </summary>
        string KnownTag { get; }
    }
}
