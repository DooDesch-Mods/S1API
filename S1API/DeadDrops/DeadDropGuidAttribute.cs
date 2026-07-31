using System;

namespace S1API.DeadDrops
{
    /// <summary>
    /// Associates a typed dead-drop identifier with a native scene GUID.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class DeadDropGuidAttribute : Attribute
    {
        public DeadDropGuidAttribute(string guid)
        {
            if (!System.Guid.TryParse(guid, out System.Guid parsed))
                throw new ArgumentException("The dead-drop GUID is invalid.", nameof(guid));

            Guid = parsed.ToString("D");
        }

        public string Guid { get; }
    }
}
