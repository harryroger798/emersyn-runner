#!/usr/bin/env python3
"""
screenshot_regression.py
Compares key screenshots between builds to detect visual regressions.
Flags: black screens, missing UI, broken rendering, major asset disappearance.
"""

import argparse
import os
import sys
import json
from pathlib import Path

try:
    from PIL import Image, ImageChops, ImageStat
    HAS_PIL = True
except ImportError:
    HAS_PIL = False
    print("WARNING: Pillow not installed. Install with: pip install Pillow")


def compute_diff_percentage(img1_path: str, img2_path: str) -> float:
    """Compute pixel difference percentage between two images."""
    if not HAS_PIL:
        return -1.0

    img1 = Image.open(img1_path).convert("RGB")
    img2 = Image.open(img2_path).convert("RGB")

    # Resize to same dimensions if needed
    if img1.size != img2.size:
        img2 = img2.resize(img1.size, Image.LANCZOS)

    diff = ImageChops.difference(img1, img2)
    stat = ImageStat.Stat(diff)

    # Average difference across all channels (0-255)
    avg_diff = sum(stat.mean) / len(stat.mean)
    return (avg_diff / 255.0) * 100.0


def is_black_screen(img_path: str, threshold: float = 5.0) -> bool:
    """Check if an image is mostly black (potential crash/black screen)."""
    if not HAS_PIL:
        return False

    img = Image.open(img_path).convert("RGB")
    stat = ImageStat.Stat(img)
    avg_brightness = sum(stat.mean) / len(stat.mean)
    return avg_brightness < threshold


def is_mostly_uniform(img_path: str, threshold: float = 3.0) -> bool:
    """Check if image is mostly one color (potential rendering failure)."""
    if not HAS_PIL:
        return False

    img = Image.open(img_path).convert("RGB")
    stat = ImageStat.Stat(img)
    avg_stddev = sum(stat.stddev) / len(stat.stddev)
    return avg_stddev < threshold


def check_ui_elements(img_path: str) -> dict:
    """Basic check for UI elements by checking if corners/edges have content."""
    if not HAS_PIL:
        return {"has_ui": True, "details": "PIL not available"}

    img = Image.open(img_path).convert("RGB")
    w, h = img.size

    # Sample regions where UI typically appears
    regions = {
        "top_left": img.crop((0, 0, w // 4, h // 8)),
        "top_right": img.crop((3 * w // 4, 0, w, h // 8)),
        "bottom_center": img.crop((w // 4, 7 * h // 8, 3 * w // 4, h)),
        "center": img.crop((w // 3, h // 3, 2 * w // 3, 2 * h // 3)),
    }

    results = {}
    for name, region in regions.items():
        stat = ImageStat.Stat(region)
        avg_stddev = sum(stat.stddev) / len(stat.stddev)
        results[name] = avg_stddev > 5.0  # Has visual content

    has_ui = any(results.values())
    return {"has_ui": has_ui, "regions": results}


def compare_builds(baseline_dir: str, current_dir: str, tolerance: float = 10.0) -> dict:
    """Compare screenshots between two builds."""
    report = {
        "baseline_dir": baseline_dir,
        "current_dir": current_dir,
        "tolerance": tolerance,
        "comparisons": [],
        "issues": [],
        "passed": True,
    }

    baseline_files = {f.name: f for f in Path(baseline_dir).glob("*.png")}
    current_files = {f.name: f for f in Path(current_dir).glob("*.png")}

    # Check current screenshots for issues
    for name, filepath in current_files.items():
        filepath_str = str(filepath)

        # Black screen check
        if is_black_screen(filepath_str):
            issue = f"BLACK SCREEN detected: {name}"
            report["issues"].append(issue)
            report["passed"] = False

        # Uniform color check
        if is_mostly_uniform(filepath_str):
            issue = f"UNIFORM COLOR (possible rendering failure): {name}"
            report["issues"].append(issue)
            report["passed"] = False

        # UI check
        ui_result = check_ui_elements(filepath_str)
        if not ui_result["has_ui"]:
            issue = f"MISSING UI elements: {name}"
            report["issues"].append(issue)
            report["passed"] = False

        # Compare with baseline if available
        if name in baseline_files:
            diff_pct = compute_diff_percentage(str(baseline_files[name]), filepath_str)
            comparison = {
                "filename": name,
                "diff_percentage": round(diff_pct, 2),
                "within_tolerance": diff_pct <= tolerance,
            }
            report["comparisons"].append(comparison)

            if diff_pct > tolerance:
                issue = f"VISUAL REGRESSION: {name} ({diff_pct:.1f}% diff, tolerance: {tolerance}%)"
                report["issues"].append(issue)
                report["passed"] = False
        else:
            report["comparisons"].append({
                "filename": name,
                "diff_percentage": -1,
                "within_tolerance": True,
                "note": "No baseline (new screenshot)",
            })

    # Check for missing screenshots (in baseline but not current)
    for name in baseline_files:
        if name not in current_files:
            issue = f"MISSING SCREENSHOT: {name} (present in baseline, missing in current)"
            report["issues"].append(issue)
            report["passed"] = False

    return report


def generate_markdown_report(report: dict, output_path: str):
    """Generate a Markdown report from comparison results."""
    lines = [
        "# Visual Regression Report",
        "",
        f"**Baseline:** {report['baseline_dir']}",
        f"**Current:** {report['current_dir']}",
        f"**Tolerance:** {report['tolerance']}%",
        f"**Result:** {'PASSED' if report['passed'] else 'FAILED'}",
        "",
    ]

    if report["issues"]:
        lines.append("## Issues Found")
        for issue in report["issues"]:
            lines.append(f"- {issue}")
        lines.append("")

    if report["comparisons"]:
        lines.append("## Screenshot Comparisons")
        lines.append("| Screenshot | Diff % | Status |")
        lines.append("|-----------|--------|--------|")
        for comp in report["comparisons"]:
            status = "PASS" if comp["within_tolerance"] else "FAIL"
            diff = f"{comp['diff_percentage']:.1f}%" if comp["diff_percentage"] >= 0 else "N/A"
            note = comp.get("note", "")
            lines.append(f"| {comp['filename']} | {diff} | {status} {note} |")
        lines.append("")

    with open(output_path, "w") as f:
        f.write("\n".join(lines))

    print(f"Report written to {output_path}")


def main():
    parser = argparse.ArgumentParser(description="Screenshot visual regression checker")
    parser.add_argument("--baseline", "-b", help="Baseline screenshots directory")
    parser.add_argument("--current", "-c", required=True, help="Current screenshots directory")
    parser.add_argument("--tolerance", "-t", type=float, default=10.0, help="Diff tolerance percentage")
    parser.add_argument("--output", "-o", help="Output report path (Markdown)")
    parser.add_argument("--json", help="Output report path (JSON)")

    args = parser.parse_args()

    if not os.path.isdir(args.current):
        print(f"ERROR: Current directory not found: {args.current}")
        sys.exit(1)

    # If no baseline, just check current screenshots for issues
    if args.baseline and not os.path.isdir(args.baseline):
        print(f"WARNING: Baseline directory not found: {args.baseline}")
        args.baseline = None

    report = compare_builds(
        baseline_dir=args.baseline or "",
        current_dir=args.current,
        tolerance=args.tolerance,
    )

    # Output
    print(f"\nResult: {'PASSED' if report['passed'] else 'FAILED'}")
    if report["issues"]:
        print(f"Issues ({len(report['issues'])}):")
        for issue in report["issues"]:
            print(f"  - {issue}")

    if args.output:
        generate_markdown_report(report, args.output)

    if args.json:
        with open(args.json, "w") as f:
            json.dump(report, f, indent=2)
        print(f"JSON report: {args.json}")

    sys.exit(0 if report["passed"] else 1)


if __name__ == "__main__":
    main()
