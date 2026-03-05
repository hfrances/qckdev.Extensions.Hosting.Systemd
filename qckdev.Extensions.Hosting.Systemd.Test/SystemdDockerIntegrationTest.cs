using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace qckdev.Extensions.Hosting.Systemd.Test
{
    [TestClass]
    public class SystemdDockerIntegrationTest
    {
        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("Docker")]
        [TestCategory("Systemd")]
        public async Task Docker_Systemd_Service_Should_Be_Active_And_Respond_Http200()
        {
            var enabled = Environment.GetEnvironmentVariable("RUN_SYSTEMD_DOCKER_TESTS");
            if (!string.Equals(enabled, "1", StringComparison.Ordinal))
            {
                Assert.Inconclusive("Set RUN_SYSTEMD_DOCKER_TESTS=1 to enable this test.");
                return;
            }

            if (!TryRunProcess("docker", "version", 30000, out _, out _, out var dockerVersionExitCode))
            {
                Assert.Inconclusive("Docker CLI no disponible en PATH.");
                return;
            }

            if (dockerVersionExitCode != 0)
            {
                Assert.Inconclusive("Docker daemon no está disponible.");
                return;
            }

            var moduleRoot = FindModuleRoot();
            if (moduleRoot is null)
            {
                Assert.Fail("No se encontró la carpeta raíz del módulo qckdev.Extensions.Hosting.Systemd.");
                return;
            }

            var tag = "miapp-core:systemd-test";
            var dockerfile = Path.Combine(moduleRoot, "miapp-core", "Dockerfile.systemd");
            var buildArgs = "build -f \"" + dockerfile + "\" -t " + tag + " \"" + moduleRoot + "\"";

            var built = TryRunProcess("docker", buildArgs, 1800000, out var buildStdout, out var buildStderr, out var buildExitCode);
            Assert.IsTrue(built, "No se pudo iniciar docker build.");
            Assert.AreEqual(0, buildExitCode, "docker build falló.\nSTDOUT:\n" + buildStdout + "\nSTDERR:\n" + buildStderr);

            var containerName = "miapp-core-systemd-test-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var hostPort = 18080;
            var runArgs = "run --rm -d --name " + containerName +
                          " --privileged --cgroupns=host -v /sys/fs/cgroup:/sys/fs/cgroup:rw -p " + hostPort + ":80 " + tag;

            try
            {
                var started = TryRunProcess("docker", runArgs, 120000, out var runStdout, out var runStderr, out var runExitCode);
                Assert.IsTrue(started, "No se pudo iniciar docker run.");

                if (runExitCode != 0)
                {
                    if (ContainsEnvironmentLimitation(runStderr))
                    {
                        Assert.Inconclusive("El entorno Docker actual no soporta systemd dentro del contenedor: " + runStderr);
                        return;
                    }

                    Assert.Fail("docker run falló.\nSTDOUT:\n" + runStdout + "\nSTDERR:\n" + runStderr);
                }

                var active = await WaitUntilAsync(
                    async () =>
                    {
                        var ok = TryRunProcess("docker", "exec " + containerName + " systemctl is-active miapp-core", 20000, out var stdout, out _, out var exitCode);
                        return ok && exitCode == 0 && stdout.Trim().Equals("active", StringComparison.OrdinalIgnoreCase);
                    },
                    TimeSpan.FromSeconds(60),
                    TimeSpan.FromSeconds(2));

                Assert.IsTrue(active, "El servicio miapp-core no llegó a estado active.");

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                var url = "http://127.0.0.1:" + hostPort + "/weatherforecast";
                var response = await http.GetAsync(url);
                Assert.AreEqual(200, (int)response.StatusCode, "El endpoint /weatherforecast no devolvió HTTP 200.");
            }
            finally
            {
                TryRunProcess("docker", "stop " + containerName, 30000, out _, out _, out _);
            }
        }

        private static string? FindModuleRoot()
        {
            var current = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(current))
            {
                var candidate = Path.Combine(current, "miapp-core", "Dockerfile.systemd");
                if (File.Exists(candidate))
                {
                    return current;
                }

                var parent = Directory.GetParent(current);
                if (parent is null)
                {
                    return null;
                }

                current = parent.FullName;
            }

            return null;
        }

        private static bool ContainsEnvironmentLimitation(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return false;
            }

            var text = error.ToLowerInvariant();
            return text.Contains("operation not permitted")
                || text.Contains("privileged")
                || text.Contains("cgroup")
                || text.Contains("permission denied");
        }

        private static async Task<bool> WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan delay)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (await condition())
                {
                    return true;
                }

                await Task.Delay(delay);
            }

            return false;
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
                var stdoutBuilder = new StringBuilder();
                var stderrBuilder = new StringBuilder();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    process.StartInfo.Environment["DOCKER_CLI_HINTS"] = "false";
                }

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stdoutBuilder.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        stderrBuilder.AppendLine(e.Data);
                    }
                };

                if (!process.Start())
                {
                    return false;
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

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

                process.WaitForExit();
                stdout = stdoutBuilder.ToString();
                stderr = stderrBuilder.ToString();
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
