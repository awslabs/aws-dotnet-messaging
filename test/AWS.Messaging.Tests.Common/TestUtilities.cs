// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Messaging.Tests.Common;

public class TestUtilities
{
    /// <summary>
    /// Recursively searchs upward from the specified path until the target file or directory is found
    /// </summary>
    /// <param name="path">Current location</param>
    /// <param name="fileOrDirectoryName">Name of a file or directory that is a parent of <see cref="path"/> to search for</param>
    /// <returns>The path of the target destination if it is found and exists, or else an empty string if it wasn't found</returns>
    public static string FindParentDirectoryWithName(string path, string fileOrDirectoryName)
    {
        var fullPath = Path.Combine(path, fileOrDirectoryName);
        if (File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            return fullPath;
        }
        else
        {
            var parentDirectory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parentDirectory))
            {
                return string.Empty;
            }
            else
            {
                return FindParentDirectoryWithName(parentDirectory, fileOrDirectoryName);
            }
        }
    }

    /// <summary>
    /// Get the path of the deployment package for testing the custom runtime.
    /// This assumes that the 'dotnet lambda package -c Release' command was run as part of the pre-build of this csproj.
    /// </summary>
    /// <returns></returns>
    public static string GetDeploymentZipPath(string functionName)
    {
        var testsProjectDirectory = FindParentDirectoryWithName(Directory.GetParent(typeof(TestUtilities).Assembly.Location)!.FullName, "test");
        if (string.IsNullOrEmpty(testsProjectDirectory))
        {
            throw new Exception("No test project directory found");
        }

        var deploymentZipFile = Path.Combine(testsProjectDirectory, $"{functionName}\\bin\\Release\\net8.0\\{functionName}.zip".Replace('\\', Path.DirectorySeparatorChar));

        if (!File.Exists(deploymentZipFile))
        {
            throw new Exception("No deployment package zip found");
        }

        return deploymentZipFile;
    }

    /// <summary>
    /// Waits until a provided function returns true, up to a maximum time limit.
    /// </summary>
    /// <param name="matchFunction">Function to wait for returning true</param>
    /// <param name="sleepSeconds">How many seconds to delay between invocations of matchFunction</param>
    /// <param name="maxWaitSeconds">Maximum number of seconds to wait for matchFunction to return true</param>
    /// <param name="failIfNotCompleted">If true, throws an exception if maxWaitSeconds is reached. Otherwise returns.</param>
    public static async Task WaitUntilAsync(Func<Task<bool>> matchFunction, int sleepSeconds = 5, int maxWaitSeconds = 300, bool failIfNotCompleted = true)
    {
        if (sleepSeconds < 0) throw new ArgumentOutOfRangeException("sleepSeconds");
        if (maxWaitSeconds < 0) throw new ArgumentOutOfRangeException("maxWaitSeconds");

        var sleepTime = TimeSpan.FromSeconds(sleepSeconds);
        var maxTime = TimeSpan.FromSeconds(maxWaitSeconds);
        var endTime = DateTime.Now + maxTime;

        while (DateTime.Now < endTime)
        {
            if (await matchFunction())
                return;

            await Task.Delay(sleepTime);
        }

        if (failIfNotCompleted)
            throw new TimeoutException(string.Format("Wait condition was not satisfied for {0} seconds", maxWaitSeconds));
    }
}
