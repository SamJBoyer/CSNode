using System;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class BuildScript
{
    public static void Build()
    {
        Console.WriteLine("building");
        string[] args = System.Environment.GetCommandLineArgs();
        string buildPath = args[args.Length-1]; //build path interpeted from build.sh
        string buildScene = args[args.Length-3]; 

        string[] scenes = { $"Assets/Scenes/{buildScene}.unity" }; // Adjust the scene path as needed
        BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.StandaloneLinux64, BuildOptions.None);
    }
}
