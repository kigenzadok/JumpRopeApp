# JumpRopeApp

A cross-platform jump rope interval timer, motion detector, and daily step/activity tracker built with **.NET 10 MAUI**, local **SQLite** persistence, and background synchronization to **MongoDB Atlas**.

## Key Features

- **Interval Workout Timer (`MainPage`)**: Configurable jump duration, rest duration, total sets, target RPM cadence, quick presets (*Beginner*, *HIIT Burn*, *Endurance*), custom canvas ring (`TimerRingDrawable`), text-to-speech cues, haptic feedback, and a completion summary modal.
- **Real-Time Jump & Step Tracking**:
  - Live jump detection via `MotionDetectorService`.
  - Daily step tracking and distance/calorie/goal progress (`StepForegroundService` on Android with delta accumulation and `Preferences` persistence).
- **Custom Routines (`RoutinesPage`)**: Create and manage routines with target steps and target jumps.
- **Analytics, Personal Records & Profile (`StatsPage`)**:
  - Daily jump goal progress bar, lifetime jumps/steps/calories/workouts/averages, and personal bests (jumps, steps, sets, streak).
  - Custom `IDrawable` charts (`JumpsChartDrawable` and `StepsChartDrawable`).
  - User profile & settings (name, weight, height, daily target goal, haptics), CSV export with share sheet, stat reset, and diagnostic log viewer (`Logger`).
- **Offline-First & Cloud Sync**: Saves workout records locally via `DatabaseService` (SQLite) and syncs to the cloud backend in the background via `CloudService`.

## Tech Stack

- **Framework**: .NET 10 MAUI (Android, iOS, MacCatalyst, Tizen, Windows)
- **Language**: C#
- **Local Storage**: SQLite (`DatabaseService`) & `Preferences`
- **Cloud Backend**: REST API syncing to MongoDB Atlas (`CloudService`)

## Android Permissions & Target SDK

Designed with Android runtime permission handling in mind (including Android 10+ `ACTIVITY_RECOGNITION` via `ActivityRecognitionPermission`, `POST_NOTIFICATIONS` on Android 13+, and foreground service health tracking via `StepForegroundService`).

## Getting Started

1. **Clone the repository**
   ```bash
   git clone [https://github.com/kigenzadok/](https://github.com/kigenzadok/)<your-repo-name>.git
   cd <your-repo-name>
