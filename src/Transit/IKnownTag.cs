namespace Sellars.Transit.Alpha
{
    [System.Runtime.InteropServices.Guid("774D26AA-EDCC-4DBC-A074-FFEA80D2EC6D")]
    public interface IKnownTag
    {
        /// <summary>
        /// The tag to use for all non-null objects,
        /// especially for writers that return the same tag for every non-null value.
        /// </summary>
        string KnownTag { get; }
    }
}
