// using UnityEditor;
// using UnityEditor.Build;
// using UnityEditor.Build.Reporting;
// using UnityEngine;

// public class BuildVersionIncrementer : IPreprocessBuildWithReport
// {
// 	public int callbackOrder => 0; // Lower values run earlier

// 	public void OnPreprocessBuild(BuildReport report)
// 	{
// 		// Get current version from PlayerSettings
// 		string currentVersion = PlayerSettings.bundleVersion;

// 		// Parse version (assuming format is Major.Minor.Patch, e.g., "1.0.0")
// 		if (TryParseVersion(currentVersion, out int major, out int minor, out int patch))
// 		{
// 			// Increment patch version
// 			patch++;

// 			// Construct new version string
// 			string newVersion = $"{major}.{minor}.{patch}";

// 			// Update PlayerSettings with new version
// 			PlayerSettings.bundleVersion = newVersion;

// 			Debug.Log($"Version incremented from {currentVersion} to {newVersion}");
// 		}
// 		else
// 		{
// 			Debug.LogWarning($"Failed to parse version: {currentVersion}. Ensure it follows Major.Minor.Patch format (e.g., 1.0.0).");
// 		}
// 	}

// 	// Helper method to parse version string
// 	private bool TryParseVersion(string version, out int major, out int minor, out int patch)
// 	{
// 		major = minor = patch = 0;
// 		string[] parts = version.Split('.');
// 		if (parts.Length != 3)
// 			return false;

// 		return int.TryParse(parts[0], out major) &&
// 			   int.TryParse(parts[1], out minor) &&
// 			   int.TryParse(parts[2], out patch);
// 	}
// }