#!/usr/bin/env python3
"""Headless checks against actual game sources; this is NOT a Unity build or playtest."""
import argparse
import ctypes.util
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tempfile

HERE = Path(__file__).resolve().parent
PLUGIN = Path("Assets/Plugins/SQLiter/DLLs/Mono.Data.SqliteClient.dll")


def version_key(path):
    return tuple(int(p) if p.isdigit() else 0 for p in path.name.split("."))


def find_dotnet(root_argument):
    executable = shutil.which("dotnet")
    configured = root_argument or os.environ.get("DOTNET_ROOT")
    if configured:
        root = Path(configured).expanduser().resolve()
    elif executable:
        root = Path(executable).resolve().parent
    else:
        raise RuntimeError("Install .NET SDK 8+ or supply --dotnet-root /path/to/dotnet.")
    dotnet = root / ("dotnet.exe" if os.name == "nt" else "dotnet")
    compilers = sorted((root / "sdk").glob("*/Roslyn/bincore/csc.dll"),
                       key=lambda p: version_key(p.parents[2]))
    runtimes = sorted((root / "shared/Microsoft.NETCore.App").glob("*"), key=version_key)
    if not dotnet.exists() or not compilers or not runtimes:
        raise RuntimeError("The configured directory must contain a complete .NET SDK 8+.")
    for runtime in reversed(runtimes):
        major = runtime.name.split(".")[0]
        if not major.isdigit() or int(major) < 8:
            continue
        packs = sorted((root / "packs/Microsoft.NETCore.App.Ref").glob(major + ".*"), key=version_key)
        if packs:
            refs = packs[-1] / "ref" / ("net" + major + ".0")
            if refs.is_dir():
                return dotnet, compilers[-1], refs, runtime.name, "net" + major + ".0"
    raise RuntimeError("A matching .NET SDK/runtime/reference pack version 8+ is required.")


def sqlite_library(project, destination):
    source = project / PLUGIN
    target = destination / PLUGIN.name
    if source.exists():
        shutil.copy2(source, target)
    else:
        # Also supports a Git sparse checkout. Git may fetch the missing blob.
        result = subprocess.run(["git", "-C", str(project), "show", "HEAD:" + PLUGIN.as_posix()],
                                stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        if result.returncode:
            raise RuntimeError("The original project's Mono.Data.SqliteClient.dll is missing.")
        target.write_bytes(result.stdout)
    if sys.platform.startswith("linux"):
        name = ctypes.util.find_library("sqlite3")
        if not name:
            raise RuntimeError("Install the operating system's SQLite 3 library.")
        machine = platform.machine()
        folders = [Path("/lib") / (machine + "-linux-gnu"),
                   Path("/usr/lib") / (machine + "-linux-gnu"), Path("/lib64"),
                   Path("/usr/lib64"), Path("/lib"), Path("/usr/lib")]
        source_native = next((folder / name for folder in folders if (folder / name).exists()), None)
        if not source_native:
            raise RuntimeError("Could not locate native SQLite library " + name)
        (destination / "libsqlite3.so").symlink_to(source_native.resolve())
    elif os.name == "nt":
        native = project / "Assets/Plugins/SQLiter/DLLs/x64/sqlite3.dll"
        if not native.exists():
            raise RuntimeError("The project's x64 sqlite3.dll is required on Windows.")
        shutil.copy2(native, destination / "sqlite3.dll")
    return target


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("project", nargs="?", type=Path, default=HERE.parents[1],
                        help="Unity project root (default: repository containing this tool)")
    parser.add_argument("--dotnet-root", help="Directory containing the dotnet host and SDK")
    args = parser.parse_args()
    project = args.project.expanduser().resolve()
    if not (project / "Assets/Scripts/RoundManager.cs").exists():
        raise RuntimeError("Not a compatible Unity project root: " + str(project))
    dotnet, compiler, refs, runtime, tfm = find_dotnet(args.dotnet_root)
    with tempfile.TemporaryDirectory(prefix="zombies-verification-") as temporary:
        temporary = Path(temporary)
        plugin = sqlite_library(project, temporary)
        env = dict(os.environ)
        env["LD_LIBRARY_PATH"] = str(temporary) + os.pathsep + env.get("LD_LIBRARY_PATH", "")
        env["ZOMBIES_TEST_DATA"] = str(temporary / "data")
        env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        groups = {
            "Waves": [project / "Assets/Scripts/RoundManager.cs"],
            "Database": [project / "Assets/Scripts/Database/Database.cs",
                         project / "Assets/Scripts/Database/GameState.cs"],
            "Gameplay": sorted((project / "Assets/Scripts").rglob("*.cs")),
        }
        for name, actual_sources in groups.items():
            output = temporary / (name + ".dll")
            command = [str(dotnet), "exec", str(compiler), "-nologo", "-noconfig", "-nostdlib",
                       "-target:exe", "-nowarn:0649,0067,0108", "-out:" + str(output)]
            command += ["-r:" + str(p) for p in refs.glob("*.dll")]
            if name != "Waves":
                command += ["-r:" + str(plugin)]
            command += [str(p) for p in sorted((HERE / name).glob("*.cs"))]
            command += [str(p) for p in actual_sources]
            print("\nCompiling actual sources for " + name + " (Unity stubs only)", flush=True)
            subprocess.run(command, check=True, env=env)
            output.with_suffix(".runtimeconfig.json").write_text(json.dumps({
                "runtimeOptions": {"tfm": tfm, "framework": {
                    "name": "Microsoft.NETCore.App", "version": runtime}}}), encoding="utf-8")
            subprocess.run([str(dotnet), str(output)], check=True, env=env)
    print("\nAll headless checks passed. A real Unity import/build/playtest is still required.")


if __name__ == "__main__":
    try:
        main()
    except (RuntimeError, subprocess.CalledProcessError) as error:
        print("Verification failed: " + str(error), file=sys.stderr)
        sys.exit(1)
