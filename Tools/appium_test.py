#!/usr/bin/env python3
"""
appium_test.py
Automated gameplay test for Emersyn Runner via Appium.
Performs swipe sequences and captures screenshots at key moments.
Designed to run on AWS Device Farm.
"""

import os
import sys
import time
import unittest
from datetime import datetime

try:
    from appium import webdriver
    from appium.webdriver.common.touch_action import TouchAction
except ImportError:
    print("WARNING: Appium not installed. Install with: pip install Appium-Python-Client")
    sys.exit(0)


class EmersynRunnerTest(unittest.TestCase):
    """Automated gameplay test for Emersyn Runner."""

    SCREENSHOT_DIR = os.environ.get("SCREENSHOT_DIR", "/tmp/screenshots")
    APP_PACKAGE = "com.emersynGames.emersynrunner"
    APP_ACTIVITY = "com.unity3d.player.UnityPlayerActivity"

    def setUp(self):
        """Set up Appium driver."""
        os.makedirs(self.SCREENSHOT_DIR, exist_ok=True)

        desired_caps = {
            "platformName": "Android",
            "automationName": "UiAutomator2",
            "appPackage": self.APP_PACKAGE,
            "appActivity": self.APP_ACTIVITY,
            "newCommandTimeout": 300,
            "noReset": True,
        }

        # Device Farm sets APPIUM_SERVER_URL
        server_url = os.environ.get("APPIUM_SERVER_URL", "http://127.0.0.1:4723/wd/hub")
        self.driver = webdriver.Remote(server_url, desired_caps)
        self.driver.implicitly_wait(10)

        # Get screen dimensions
        size = self.driver.get_window_size()
        self.screen_width = size["width"]
        self.screen_height = size["height"]

    def tearDown(self):
        """Clean up."""
        if hasattr(self, "driver") and self.driver:
            self.driver.quit()

    def save_screenshot(self, name: str):
        """Save a screenshot with timestamp."""
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        filename = f"{name}_{timestamp}.png"
        filepath = os.path.join(self.SCREENSHOT_DIR, filename)
        self.driver.save_screenshot(filepath)
        print(f"Screenshot saved: {filepath}")

    def swipe(self, direction: str, duration: int = 300):
        """Perform a swipe gesture."""
        cx = self.screen_width // 2
        cy = self.screen_height // 2
        offset = min(self.screen_width, self.screen_height) // 4

        if direction == "left":
            self.driver.swipe(cx + offset, cy, cx - offset, cy, duration)
        elif direction == "right":
            self.driver.swipe(cx - offset, cy, cx + offset, cy, duration)
        elif direction == "up":
            self.driver.swipe(cx, cy + offset, cx, cy - offset, duration)
        elif direction == "down":
            self.driver.swipe(cx, cy - offset, cx, cy + offset, duration)

    def tap_center(self):
        """Tap the center of the screen."""
        self.driver.tap([(self.screen_width // 2, self.screen_height // 2)])

    def tap_play_button(self):
        """Tap the Play button (assumed to be in the center-lower area)."""
        x = self.screen_width // 2
        y = int(self.screen_height * 0.6)
        self.driver.tap([(x, y)])

    def verify_app_foreground(self):
        """Verify the app is still in the foreground."""
        current = self.driver.current_package
        self.assertEqual(current, self.APP_PACKAGE,
                         f"App not in foreground! Current: {current}")

    def test_gameplay_sequence(self):
        """Main test: launch, play, swipe, capture screenshots."""
        print("=== Emersyn Runner Appium Test ===")
        print(f"Screen: {self.screen_width}x{self.screen_height}")

        # Step 1: Wait for app to load
        print("\n[1] Waiting for app to load...")
        time.sleep(5)
        self.save_screenshot("01_menu")
        self.verify_app_foreground()

        # Step 2: Tap Play
        print("[2] Tapping Play...")
        self.tap_play_button()
        time.sleep(2)
        self.save_screenshot("02_gameplay_start")
        self.verify_app_foreground()

        # Step 3: Gameplay swipe sequence (90 seconds)
        print("[3] Starting gameplay swipe sequence (90s)...")
        swipe_sequence = ["left", "right", "up", "down"]
        start_time = time.time()
        swipe_count = 0
        screenshot_times = [30, 60]
        screenshots_taken = set()

        while time.time() - start_time < 90:
            elapsed = time.time() - start_time

            # Take screenshots at key times
            for t in screenshot_times:
                if elapsed >= t and t not in screenshots_taken:
                    self.save_screenshot(f"03_gameplay_{t}s")
                    self.verify_app_foreground()
                    screenshots_taken.add(t)

            # Perform swipe
            direction = swipe_sequence[swipe_count % len(swipe_sequence)]
            try:
                self.swipe(direction)
                swipe_count += 1
            except Exception as e:
                print(f"  Swipe failed: {e}")

            # Small delay between swipes
            time.sleep(1.5)

        print(f"  Completed {swipe_count} swipes in 90s")

        # Step 4: Final screenshot
        self.save_screenshot("04_gameplay_90s")
        self.verify_app_foreground()

        # Step 5: Force collision (swipe into obstacle by not avoiding)
        print("[4] Waiting for game over...")
        time.sleep(10)
        self.save_screenshot("05_game_over")

        # Verify app didn't crash
        self.verify_app_foreground()
        print("\n=== Test Complete: App remained stable ===")


if __name__ == "__main__":
    unittest.main()
