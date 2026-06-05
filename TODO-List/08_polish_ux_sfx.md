# Visual Polish, UX & Audio System Checklist

Checklist for high-end aesthetics, micro-animations, screen transitions, particle effects, font localization optimization, and sound designs (SFX/BGM) to deliver a premium hyper-casual game feel.

## 1. Visual Presentation & Transitions (MVP)
- [x] **Safe Area Adjuster**: Automatically scales UI bounds to avoid mobile screen notches or home indicators.
  - Reference: [SafeAreaHandler.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Core/SafeAreaHandler.cs)
- [x] **Scene Transition Effects**: SlideUp, SlideDown, and Fade transitions between Boot, Lobby, and InGame scenes.
  - Reference: [SceneTransition.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Core/SafeAreaHandler.cs)
- [x] **Smooth Gravity Drop Animations**: Compacting blocks fall down smoothly over time rather than snapping instantly.
  - Reference: [BoardView.cs:PlayGravity](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/View/BoardView.cs)
- [/] **Particle VFX Feedback**: Explosion effects for Bomb blast, streak sweeps for Rockets, and matching sprite pop sequences on basic cell removals.
  - Status: Basic particles implemented in [BoardView.cs:PlayRemovalEffects](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/InGame/View/BoardView.cs). Needs additional visual flash and juicy explosion particles to match standard premium hyper-casual games (e.g. from Voodoo or SayGames).

## 2. Localization & Font Optimization (MVP)
- [x] **Font Subsetting Tool**: Automation script to build compact font subsets for 15 supported languages, optimizing texture storage and build sizes.
  - Reference: [subset_fonts.bat](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/subset_fonts.bat) and [subset_fonts.js](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/tools/subset_tool/subset_fonts.js)
- [x] **Localization Service**: Handles string replacements for multiple locales during runtime.
  - Reference: [LocalizationService.cs](file:///c:/Users/SangHyeok/Desktop/git/madalang-games/project-flood/client/project-flood/Assets/Scripts/Services/LocalizationService.cs)

## 3. Phase N (Juice, Polish & Audio Integration)
- [ ] **Sound Manager (SFX/BGM)**: Singleton audio manager handling volume parameters and playing BGM tracks plus dynamic SFX clips (tap pop, bomb blast, rocket travel, gold coin collection, star unlock sounds, fail overlay chime).
- [ ] **Micro-animations (Button Juice)**: Add scale bounces on button presses, hover glows, progress bar slides, and shaking animations on illegal moves.
- [ ] **Celebration Screen Polish**: Add falling confetti particles, star-slam sequences, and counter rolls for gold rewards on the stage result screen.
- [ ] **Vaptic Haptic Feedback**: Trigger subtle device vibrations (light tap, medium blast, heavy shake) on mobile platforms matching in-game actions.
- [ ] **Dynamic Backgrounds**: Animate water levels rising or falling in the background to visual represent stage clearance progress.
