using System;

namespace uk.novavoidhowl.dev.cvrfury.compiled.vrccontacts
{
  public static class APIVersion
  {
    private static readonly Version _version = new Version(1, 0, 0);

    // For serialization and display
    public static string CurrentVersion
    {
      get { return _version.ToString(); }
    }

    // For version comparison
    public static Version AsVersion
    {
      get { return _version; }
    }

    // Helper method for version checking
    public static bool IsVersionAtLeast(Version otherVersion)
    {
      return _version.CompareTo(otherVersion) >= 0;
    }

    public static bool IsVersionAtLeast(string otherVersion)
    {
      return IsVersionAtLeast(new Version(otherVersion));
    }
  }
}
