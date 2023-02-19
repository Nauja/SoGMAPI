from __future__ import annotations
import os
from typing import TYPE_CHECKING
from pathlib import Path
import re
import logging

if TYPE_CHECKING:
    from typing import Union, Callable

    StrPath = Union[str, Path]
    FileFilter = Callable[[Path], bool]

# Directory containing SMAPI repository
SMAPI = "SMAPI"
SMAPI_DIR = Path.cwd() / "vendor" / SMAPI

# Directories of SMAPI to copy
SMAPI_DIRS = ("src", "build", "docs")

# Directory where to generate SoGMAPI
SOGMAPI = "SoGMAPI"
SOGMAPI_DIR = Path.cwd()

SMAPI_PROJECTS = {SMAPI: SOGMAPI} | {
    f"{SMAPI}.{project}": f"{SOGMAPI}.{project}"
    for project in (
        "Installer",
        "Internal",
        "Internal.Patching",
        "ModBuildConfig",
        "ModBuildConfig.Analyzer",
        "ModBuildConfig.Analyzer.Tests",
        "Mods.ConsoleCommands",
        "Mods.ErrorHandler",
        "Mods.SaveBackup",
        "Toolkit",
        "Toolkit.CoreInterfaces",
    )
}


class Action:
    def __init__(self, filter: FileFilter | None = None) -> None:
        self.filter = filter

    def matches(self, path: Path) -> bool:
        return self.filter(path) if self.filter else False

    def do(self, path: Path, content: str) -> str:
        return content


class ReplaceText(Action):
    def __init__(self, hint: str, a: str, b: str) -> None:
        super(ReplaceText, self).__init__(filter=FILTER_TEXT)
        self.hint = hint
        self.a = a
        self.b = b

    def do(self, path: Path, content: str) -> str:
        return content.replace(self.a, self.b)


class RemoveRegex(Action):
    def __init__(self, hint: str, pattern: str) -> None:
        super(RemoveRegex, self).__init__(filter=FILTER_TEXT)
        self.hint = hint
        self.regex = re.compile(pattern, re.DOTALL | re.MULTILINE)

    def do(self, path: Path, content: str) -> str:
        return self.regex.sub("", content)


class ActionList(Action):
    def __init__(self, /, actions: Action | list[Action] | None = None) -> None:
        super(ActionList, self).__init__()

        if actions is None:
            actions = []
        elif isinstance(actions, Action):
            actions = [actions]

        self.actions: list[Action] = list(actions)

    def add(self, action: Action) -> None:
        self.actions.append(action)

    def matches(self, path: Path) -> bool:
        return any([a.matches(path) for a in self.actions])

    def do(self, path: Path, content: str) -> str:
        for a in self.actions:
            if a.matches(path):
                content = a.do(path, content)

        return content


FILTER_CODE = lambda path: str(path).endswith(".cs")
FILTER_TEXT = lambda path: str(path).endswith(
    (
        ".cs",
        ".md",
        ".ps1",
        ".txt",
        ".targets",
        ".sh",
        ".sln",
        ".csproj",
        ".json",
        ".bat",
        ".projitems",
        ".shproj",
    )
)


def all_filters(filters: list[FileFilter | None]) -> FileFilter:
    def wrapper(path: Path) -> bool:
        for filter in filters:
            if filter and not filter(path):
                return False

        return True

    return wrapper


def walk_files(dir: StrPath, fun: Callable[[Path], None], filter: FileFilter) -> None:
    for root, dirs, files in os.walk(dir):
        for file in files:
            src = Path(root) / file
            if not filter(src):
                continue

            fun(src)


def all_files(fun: Callable[[Path], None], filter: FileFilter) -> None:
    for dir in ("src", "build"):
        walk_files(dir, fun, filter)


def all_files_content(
    action: Action, filter: FileFilter | None = None, verbose: bool | None = None
) -> None:
    def wrapper(path: Path) -> None:
        if verbose:
            logging.debug(f"Patch {path}...")

        with open(path, encoding="utf-8") as f:
            content = f.read()

        content = action.do(path, content)

        with open(path, "w", encoding="utf-8") as f:
            f.write(content)

    all_files(wrapper, all_filters([filter, action.matches]))
