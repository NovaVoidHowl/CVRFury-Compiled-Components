using System;

namespace uk.novavoidhowl.dev.cvrfury.compiled.vrccolliders
{
  public static class APIVersion
  {
    private static readonly Version _version = new Version(1, 0, 0);

    public static string CurrentVersion
    {
      get { return _version.ToString(); }
    }

    public static Version AsVersion
    {
      get { return _version; }
    }

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
