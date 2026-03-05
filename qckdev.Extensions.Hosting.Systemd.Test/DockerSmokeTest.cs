using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace qckdev.Extensions.Hosting.Systemd.Test
{
    [TestClass]
    public class DockerSmokeTest
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void Docker_Run_Alpine_And_EmitExpectedOutput()
        {
            if (!TryRunProcess("docker", "version", 15000, out _, out _, out var versionExitCode))
            {
                Assert.Inconclusive("Docker CLI no disponible en PATH.");
                return;
            }

            if (versionExitCode != 0)
            {
                Assert.Inconclusive("Docker daemon no está disponible.");
                return;
            }

            var image = "alpine:3.20";
            var marker = "qckdev-docker-ok";
            var args = "run --rm " + image + " sh -c \"echo " + marker + "\"";

            var started = TryRunProcess("docker", args, 120000, out var stdout, out var stderr, out var runExitCode);
            Assert.IsTrue(started, "No se pudo iniciar el proceso docker.");
            Assert.AreEqual(0, runExitCode, "docker run devolvió error. stderr: " + stderr);
            StringAssert.Contains(stdout, marker);
        }

        private static bool TryRunProcess(string fileName, string arguments, int timeoutMs, out string stdout, out string stderr, out int exitCode)
        {
            stdout = string.Empty;
            stderr = string.Empty;
            exitCode = -1;

            try
            {
                using var process = new Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    process.StartInfo.Environment["DOCKER_CLI_HINTS"] = "false";
                }

                if (!process.Start())
                {
                    return false;
                }

                if (!process.WaitForExit(timeoutMs))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                    }

                    stderr = "Timeout esperando a que finalice el proceso.";
                    return true;
                }

                stdout = process.StandardOutput.ReadToEnd();
                stderr = process.StandardError.ReadToEnd();
                exitCode = process.ExitCode;
                return true;
            }
            catch (Exception ex)
            {
                stderr = ex.Message;
                return false;
            }
        }
    }
}
