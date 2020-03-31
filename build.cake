//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=5.2.0"
#addin "nuget:?package=Cake.Docker&version=0.10.0"
#addin "nuget:?package=Cake.Incubator&version=5.1.0"

using Cake.Incubator.LoggingExtensions;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

// This is the verbosity of the Cake logs - by defaulting to Normal we get better logging out of the gate
Context.Log.Verbosity = Argument("Verbosity", Verbosity.Normal);

var target = Argument("target", "Default");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
string semVer;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    var fromEnv = context.EnvironmentVariable("GitVersion.semVer");
    
    if (string.IsNullOrEmpty(fromEnv))
    { 
        var gitVersionInfo = GitVersion(new GitVersionSettings {
            OutputType = GitVersionOutput.Json
        });
        semVer = gitVersionInfo.SemVer;
        Information("Building step-execution-container images v{0}", semVer);
        Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
        Verbose("GitVersion:\n{0}", gitVersionInfo.Dump());
    }
    else
    {
        semVer = fromEnv;
        Information("Building step-execution-container images v{0}", semVer);
    }

    if (BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(semVer);
});

Teardown(context =>
{
    Information("Finished running tasks for build v{0}", semVer);
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .Does(() =>
{
    var tag = $"octopusdeploy/step-execution-container:{semVer}-ubuntu1804";
    DockerBuild(new DockerImageBuildSettings { Tag = new [] { tag } }, "ubuntu.18.04");
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);