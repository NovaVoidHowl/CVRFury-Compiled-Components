using System;

namespace uk.novavoidhowl.dev.cvrfury.compiled.vrcconstraints
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
      return _version >= otherVersion;
    }

    public static bool IsVersionAtLeast(string versionString)
    {
      Version otherVersion;
      if (Version.TryParse(versionString, out otherVersion))
      {
        return IsVersionAtLeast(otherVersion);
      }
      return false;
    }
  }
}
