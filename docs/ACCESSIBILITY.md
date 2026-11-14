# Accessibility

Learn With Laura is designed for keyboard, controller/switch-style navigation, screen readers, larger text, higher contrast, reduced motion, and play without audio. Accessibility settings are stored locally and never leave the machine.

## Input and focus

Every interactive screen uses a focus-following vertical scroll container. The first available control receives focus when a screen opens, and focus is shown with a five-pixel gold outline.

- Keyboard: `Tab` and `Shift+Tab` move through controls; arrow keys adjust lists, sliders, grade, and option controls; `Enter` or `Space` activates the focused control.
- Controller or switch adapter configured as a controller: directional pad/left stick moves focus; the primary action button activates it. This provides a one-direction-plus-activate scanning path through each flow without pointer input.
- Mouse and touch remain available but are not required.

Focus order follows the visual/top-to-bottom scene order. Scroll containers use `follow_focus`, so focused controls remain visible at 1024×768 with Larger Text enabled.

## Visual access

Larger Text adds six pixels to the shared type scale and uses Atkinson Hyperlegible. Higher Contrast uses white or warm-yellow text, an opaque near-black panel, a gold border, and the same visible focus outline. Godot 4.8 operating-system high-contrast and reduced-animation preferences are honored automatically in addition to the local toggles.

Visual verification checklist:

1. Test 1024×768 and 1536×1024 in windowed mode.
2. Enable Larger Text and Higher Contrast together.
3. Traverse every control using only `Tab`, `Shift+Tab`, arrows, and activation keys.
4. Confirm no text truncates, every focused control scrolls into view, status text wraps, and focus never relies on color alone.
5. Repeat the route with controller directional navigation and the primary action button.

Automated layout tests enforce scroll/focus behavior on Main Menu, learner selection, grade, subject, settings, lessons, adventure, progress, and learning-break screens.

## Motion, patterns, and audio

Reduce Motion replaces the animated/audio memory playback with a persistent numbered text sequence, such as `1. Creek → 2. Camp → 3. Deer Track`. The learner selects the same labeled buttons in order while the text remains visible. Audio cues are supplemental in both modes; labels, feedback, scoring, retry behavior, and completion never require sound.

The static alternative activates when either the local Reduce Motion setting or the operating system's reduced-animation preference is on. The setting also shortens skippable splash delays.

## Screen readers

Native Godot accessibility support is explicitly enabled. Each window and screen has an accessible name and navigation description. Standard controls expose their visible labels; unlabeled sliders, lists, and text inputs receive generated names. Feedback, status, and progress labels are polite live regions so changes are announced without interrupting the current control.

The current development target is macOS with VoiceOver. Before a distributable platform is declared supported by the release process, run the following with that platform's built-in reader (VoiceOver on macOS, Narrator on Windows, or Orca on Linux):

1. Launch with the screen reader already active and verify the window/screen name.
2. Complete learner selection, grade/subject selection, one correct lesson answer, one hint/correction, one skip, a static memory activity, a learning break, progress review, and return home.
3. Confirm control role/name/state/value, focus order, live feedback, dialog buttons, disabled controls, and selected list items are announced.
4. Repeat with all audio muted and Reduce Motion enabled.
5. Record OS, reader version, game commit, tester, failures, and retest result in the release evidence.

Automated tests verify the accessibility project setting, live-region wiring, operating-system preference logic, static pattern text, and focus-following layouts. Automated checks do not replace assistive-technology or disabled-user testing.

## Release gate and review

Public release remains blocked until compensated blind/low-vision and disability reviewers complete the screen-reader, contrast, scalable-text, switch/controller, non-audio, motion, and timing checklist. Findings must be tracked to closure, and the reviewer must be invited to verify fixes. Windows or Linux must not be listed as supported until its reader matrix is completed after export presets are established.

This gate complements the disability, accessibility, cultural, educator, and bias review requirements in `content/CONTENT_NOTES.md`.
