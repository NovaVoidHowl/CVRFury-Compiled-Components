using System;

namespace uk.novavoidhowl.dev.cvrfury.VRCAVstub.Common
{
  public static class StubVersion
  {
    private static readonly Version _version = new Version(1, 0, 1);

    // For serialization and display
    public static string CurrentVersion => _version.ToString();

    // For version comparison
    public static Version AsVersion => _version;

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
