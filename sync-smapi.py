from __future__ import annotations
from pathlib import Path
import os
import sys
import importlib.util
from typing import TYPE_CHECKING
import shutil
import logging
from patches import SMAPI_DIR, SMAPI_DIRS, SOGMAPI_DIR, walk_files, all_files_content

if TYPE_CHECKING:
    from typing import Union, Callable
    from patches import ActionList

    StrPath = Union[str, Path]
    FileFilter = Callable[[Path], bool]

logging.basicConfig(level=logging.DEBUG)

sys.path.append(str(SOGMAPI_DIR))

# Directory containing the patches
PATCHES_DIR = SOGMAPI_DIR / "patches"

# Files and directories to remove
SMAPI_DONT_KEEP = (
    [
        "build/unix",
        "src/SoGMAPI.ModBuildConfig",
        "src/SoGMAPI.ModBuildConfig.Analyzer",
        "src/SoGMAPI.ModBuildConfig.Analyzer.Tests",
        "src/SoGMAPI.Tests",
        "src/SoGMAPI.Tests.ModApiConsumer",
        "src/SoGMAPI.Tests.ModApiProvider",
        "src/SoGMAPI.Web",
        "src/SoGMAPI.Web.LegacyRedirects",
        "src/SoGMAPI.Installer/assets/install on macOS.command",
        "src/SoGMAPI.Installer/assets/install on Linux.sh",
        "src/SoGMAPI.sln.DotSettings",
    ]
    + [f"src/SoGMAPI/{name}" for name in ("IAssetDataForMap.cs")]
    + [
        f"src/SoGMAPI/Framework/{name}"
        for name in ("WatcherCore.cs", "SMultiplayer.cs")
    ]
    + [f"src/SoGMAPI/Framework/ModHelpers/{name}" for name in ("MultiplayerHelper.cs",)]
    + [
        f"src/SoGMAPI/Framework/Networking/{name}"
        for name in (
            "MessageType.cs",
            "ModMessageModel.cs",
            "MultiplayerPeer.cs",
            "MultiplayerPeerMod.cs",
            "SLidgrenServer.cs",
            "SLidgrenClient.cs",
            "RemoteContextModel.cs",
            "RemoteContextModModel.cs",
        )
    ]
    + [
        f"src/SoGMAPI/Framework/Content/{name}"
        for name in ("AssetDataForMap.cs", "TilesheetReference.cs")
    ]
    + [
        f"src/SoGMAPI/Framework/StateTracking/{name}"
        for name in (
            "ChestTracker.cs",
            "LocationTracker.cs",
            "PlayerTracker.cs",
            "WorldLocationsTracker.cs",
            "FieldWatchers/NetCollectionWatcher.cs",
            "FieldWatchers/NetDictionaryWatcher.cs",
            "FieldWatchers/NetListWatcher.cs",
            "FieldWatchers/NetValueWatcher.cs",
            "FieldWatchers/WatcherFactory.cs",
            "Snapshots/LocationSnapshot.cs",
            "Snapshots/PlayerSnapshot.cs",
            "Snapshots/WatcherSnapshot.cs",
            "Snapshots/WorldLocationsSnapshot.cs",
        )
    ]
    + [
        f"src/SoGMAPI/Framework/Networking/{name}"
        for name in ("SGalaxyNetClient.cs", "SGalaxyNetServer.cs")
    ]
    + [
        f"src/SoGMAPI/Events/{name}"
        for name in (
            "BuildingListChangedEventArgs.cs",
            "ChestInventoryChangedEventArgs.cs",
            "DebrisListChangedEventArgs.cs",
            "FurnitureListChangedEventArgs.cs",
            "InventoryChangedEventArgs.cs",
            "LargeTerrainFeatureListChangedEventArgs.cs",
            "LevelChangedEventArgs.cs",
            "LoadStageChangedEventArgs.cs",
            "LocaleChangedEventArgs.cs",
            "LocationListChangedEventArgs.cs",
            "MenuChangedEventArgs.cs",
            "NpcListChangedEventArgs.cs",
            "ObjectListChangedEventArgs.cs",
            "TerrainFeatureListChangedEventArgs.cs",
            "WarpedEventArgs.cs",
        )
    ]
)


def rename(path: StrPath) -> Path:
    dst = str(Path(path).absolute().relative_to(SMAPI_DIR))

    for a, b in (("SMAPI", "SoGMAPI"), ("smapi", "sogmapi")):
        dst = dst.replace(a, b)

    return SOGMAPI_DIR / dst


def copy_dir(path: StrPath) -> None:
    """Copy a directory from SMAPI to SoGMAPI.

    It also renames all SMAPI or smapi occurences to SoGMAPI
    or sogmapi.
    """
    logging.info(f"Copy {path}...")
    src_dir = SMAPI_DIR / path

    for root, dirs, files in os.walk(src_dir):
        for name in files:
            src = Path(root) / name
            dst = rename(src)
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy(src, dst)


def delete(path: StrPath) -> None:
    """Delete a file or directory."""
    logging.info(f"Delete {path}...")
    path = SOGMAPI_DIR / path
    if path.is_dir():
        shutil.rmtree(path)
    else:
        path.unlink()


def copy_file(src: StrPath, dst: StrPath) -> None:
    """Copy a file."""
    logging.info(f"Copy {src} -> {dst}...")
    shutil.copyfile(SOGMAPI_DIR / src, SOGMAPI_DIR / dst)


def is_text_file(path: StrPath) -> bool:
    return str(path).endswith((".cs", ".md"))


def cleanup() -> None:
    """Cleanup old SoGMAPI files."""
    for dir in SMAPI_DIRS:
        logging.info(f"Delete dir {dir}...")
        shutil.rmtree(SOGMAPI_DIR / dir, ignore_errors=True)


def copy() -> None:
    """Copy files from SMAPI to SoGMAPI."""
    # Copy directories
    for dir in SMAPI_DIRS:
        copy_dir(dir)

    # Delete unwanted files and directories
    for name in SMAPI_DONT_KEEP:
        delete(name)

    copy_file("patches/MonoGame.Framework.dll", "build/MonoGame.Framework.dll")
    copy_file("patches/MonoGame.Framework.xml", "build/MonoGame.Framework.xml")


def load_patch(filename: str) -> ActionList | None:
    """Load a patch from the patches directory."""
    logging.info(f"Load patch {filename}...")
    if "sogmapi_patch" in sys.modules:
        del sys.modules["sogmapi_patch"]
    spec = importlib.util.spec_from_file_location(
        "sogmapi_patch", PATCHES_DIR / filename
    )
    if not spec or not spec.loader:
        return None

    foo = importlib.util.module_from_spec(spec)
    sys.modules["sogmapi_patch"] = foo
    spec.loader.exec_module(foo)
    return foo.build()


def patch() -> None:
    """Apply patches on SoGMAPI."""
    # First, apply the global all.py patch
    actions = load_patch("all.py")
    if actions:
        all_files_content(actions)

    # Recursively apply all patches
    def filter(path: Path) -> bool:
        """Filter patches names.

        This excludes __init__.py and all.py files.
        """
        return path.name not in ("__init__.py", "all.py")

    def fun(path: Path) -> None:
        """Apply a patch."""
        rpath = str(path.relative_to(PATCHES_DIR))
        basename = path.name
        if basename.endswith(".py"):
            actions = load_patch(rpath)
            rpath = rpath[: rpath.index(".py")]
            if actions:
                logging.info(f"Patch {rpath}...")
                with open(rpath, encoding="utf-8") as f:
                    content = f.read()

                content = actions.do(SOGMAPI_DIR / rpath, content)

                with open(rpath, "w", encoding="utf-8") as f:
                    f.write(content)
        elif basename.endswith(".cs"):
            copy_file(f"patches/{rpath}", rpath)

    walk_files(PATCHES_DIR, fun, filter=filter)


def main() -> None:
    cleanup()
    copy()
    patch()


if __name__ == "__main__":
    main()
