# Unity package tests

Open `.ci/TestProject` using the Editor version in its `ProjectSettings/ProjectVersion.txt`.
Run the package's EditMode or PlayMode suite in Unity Test Runner.

For sample tests, run `./.ci/Prepare-Sample.ps1` from the repository root before opening the test project. Then run the `AfterHours.Tests` namespace in EditMode. To prepare a separate Unity project, pass its path with `-ProjectPath`.

Make sample changes in `Samples~/AfterHours`. The test project's local sample, Addressables settings, screenshots, and content builds are generated for local testing. The preparation script protects an existing sample copy from being overwritten.
