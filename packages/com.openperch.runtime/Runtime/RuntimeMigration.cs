namespace OpenPerch.Runtime
{
    /// <summary>
    /// Brings data serialized under an older schema up to <see cref="RuntimeSchema.Current"/>. Data from a newer schema
    /// is left as it is; the app rejects such packages before loading them.
    /// </summary>
    /// <remarks>
    /// Runs from <c>OnAfterDeserialize</c>, which Unity may call on a loading thread while the rest of the bundle is still
    /// being read. Each overload therefore touches only the fields of the object it is given, does a bounded amount of
    /// work and never calls a Unity API.
    /// </remarks>
    public static class RuntimeMigration
    {
        /// <summary>Brings a deserialized <paramref name="bone"/> up to <see cref="RuntimeSchema.Current"/>.</summary>
        /// <param name="bone">The component whose fields were just read; only its own fields change.</param>
        public static void Migrate(PerchPhysBone bone)
        {
            if (bone.schemaVersion < 1)
            {
                bone.schemaVersion = 1;
            }
        }

        /// <summary>Brings a deserialized <paramref name="collider"/> up to <see cref="RuntimeSchema.Current"/>.</summary>
        /// <param name="collider">The component whose fields were just read; only its own fields change.</param>
        public static void Migrate(PerchPhysBoneCollider collider)
        {
            if (collider.schemaVersion < 1)
            {
                collider.schemaVersion = 1;
            }
        }

        /// <summary>Brings a deserialized <paramref name="constraint"/> up to <see cref="RuntimeSchema.Current"/>.</summary>
        /// <param name="constraint">The component whose fields were just read; only its own fields change.</param>
        public static void Migrate(PerchConstraint constraint)
        {
            if (constraint.schemaVersion < 1)
            {
                constraint.schemaVersion = 1;
            }
        }

        /// <summary>Brings a deserialized <paramref name="contact"/> up to <see cref="RuntimeSchema.Current"/>.</summary>
        /// <param name="contact">The component whose fields were just read; only its own fields change.</param>
        public static void Migrate(PerchContactData contact)
        {
            if (contact.schemaVersion < 1)
            {
                contact.schemaVersion = 1;
            }
        }
    }
}
