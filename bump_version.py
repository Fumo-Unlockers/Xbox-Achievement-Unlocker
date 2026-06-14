#!/usr/bin/env python3
"""Bump the XAU tool version across the codebase.

By default the version is set to today's date in YY.MM.DD form. If the current
version is already today's date (with or without a hotfix suffix), the hotfix
counter is incremented instead (26.06.14 -> 26.06.14.1 -> 26.06.14.2 ...).

Files updated:
  - XAU/ViewModels/Pages/HomeViewModel.cs   ToolVersion (auto updater + release tag)
  - XAU/XAU.csproj                          Version, AssemblyVersion, FileVersion

Examples:
  python bump_version.py            # -> 26.06.14 (or 26.06.14.N+1 if already today)
  python bump_version.py --dry-run  # preview without writing
  python bump_version.py --set 2.8.1
"""
from __future__ import annotations

import argparse
import re
import sys
from datetime import date
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent
HOME_VM = REPO_ROOT / "XAU" / "ViewModels" / "Pages" / "HomeViewModel.cs"
CSPROJ = REPO_ROOT / "XAU" / "XAU.csproj"


def read_text(path: Path) -> tuple[str, str]:
    raw = path.read_bytes()
    text = raw.decode("utf-8")
    eol = "\r\n" if "\r\n" in text else "\n"
    return text, eol


def write_text(path: Path, text: str) -> None:
    path.write_bytes(text.encode("utf-8"))


def today_base(d: date) -> str:
    return f"{d.year % 100:02d}.{d.month:02d}.{d.day:02d}"


def current_version() -> str | None:
    if not CSPROJ.exists():
        return None
    m = re.search(r"<Version>([^<]*)</Version>", CSPROJ.read_bytes().decode("utf-8"))
    return m.group(1).strip() if m else None


def next_version(base: str, current: str | None) -> str:
    if not current or current == base:
        return base if not current else f"{base}.1"
    if current.startswith(base + "."):
        suffix = current[len(base) + 1:]
        if re.fullmatch(r"\d+", suffix):
            return f"{base}.{int(suffix) + 1}"
        return f"{base}.1"
    return base


def numeric_4part(v: str) -> str:
    parts = [p for p in v.split(".") if p != ""]
    out = []
    for p in parts:
        out.append(str(int(p)) if re.fullmatch(r"\d+", p) else p)
    while len(out) < 4:
        out.append("0")
    return ".".join(out[:4])


def set_property(text: str, name: str, val: str, eol: str) -> tuple[str, bool]:
    pattern = rf"<{name}>[^<]*</{name}>"
    new_text, n = re.subn(pattern, lambda m: f"<{name}>{val}</{name}>", text)
    if n:
        return new_text, new_text != text
    anchor = re.search(r"<Version>[^<]*</Version>[ \t]*\r?\n", text)
    if anchor:
        ins = f"    <{name}>{val}</{name}>{eol}"
        return text[:anchor.end()] + ins + text[anchor.end():], True
    return text, False


def update_home_vm(version: str, dry: bool) -> bool:
    if not HOME_VM.exists():
        print(f"  ! not found: {HOME_VM}", file=sys.stderr)
        return False
    text, _ = read_text(HOME_VM)
    pattern = r'(public static string ToolVersion = ")[^"]*(";)'
    new_text, n = re.subn(pattern, lambda m: f"{m.group(1)}{version}{m.group(2)}", text)
    if n == 0:
        print(f"  ! ToolVersion line not found in {HOME_VM.name}", file=sys.stderr)
        return False
    if new_text == text:
        return False
    print(f"  {HOME_VM.name}: ToolVersion -> {version}")
    if not dry:
        write_text(HOME_VM, new_text)
    return True


def update_csproj(version: str, dry: bool) -> bool:
    if not CSPROJ.exists():
        print(f"  ! not found: {CSPROJ}", file=sys.stderr)
        return False
    text, eol = read_text(CSPROJ)
    values = {
        "Version": version,
        "AssemblyVersion": numeric_4part(version),
        "FileVersion": numeric_4part(version),
    }
    changed = False
    for name, val in values.items():
        text, did = set_property(text, name, val, eol)
        if did:
            print(f"  {CSPROJ.name}: {name} -> {val}")
            changed = True
    if changed and not dry:
        write_text(CSPROJ, text)
    return changed


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--date", help="use a specific date (YYYY-MM-DD) instead of today")
    ap.add_argument("--set", help="set an explicit version instead of computing from the date")
    ap.add_argument("--dry-run", action="store_true", help="preview without writing files")
    args = ap.parse_args()

    cur = current_version()
    print(f"Current version: {cur}")

    if args.set:
        new = args.set.strip()
        print(f"Using explicit version: {new}")
    else:
        d = date.today()
        if args.date:
            try:
                d = date.fromisoformat(args.date)
            except ValueError:
                print(f"Invalid --date '{args.date}'. Use YYYY-MM-DD.", file=sys.stderr)
                return 1
        base = today_base(d)
        new = next_version(base, cur)
        print(f"Target base: {base}")

    print(f"New version:   {new}")
    print("Updating:")
    update_home_vm(new, args.dry_run)
    update_csproj(new, args.dry_run)

    if args.dry_run:
        print("(dry run; no files written)")
    else:
        print("Done. Commit and push to Main to cut a release.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
