# Changelog

All notable changes to this project will be documented in this file.

## 0.1.11

### Added

- New 'examples' directory to show different ways of using

### Changed

- Removed the PlatoonManager.cs class. Instead, copy one of the examples into your project as a starting point
- Changed the prototype of AddEvent to accept a payload Dictionary with generic value type

## 0.1.10

### Changed

Event sending is now split into asynchronous and synchronous. Async is used when the application is running normally and synchronous is used when the application is closing down.

## 0.1.9

### Changed

- The FlagsReady callback has been repurposed so that it is passed to the general StartSession function. This provides a way to know when it is safe to start sending events AND query flags. Null can be passed if you don't care.
- The api/init function now passes local time to the server so that the implicit $sessionBegin event has client time
  rather than server time

### Fixed

- Calls the heartbeat every 20 seconds rather than mistakenly only once
- Fixes an issue where there were two code-paths for event adding and one didn't actually store the event into the event buffer

## 0.1.8

### Added

- New 'feature flag' system

## 0.0.7

### Changed

- The name of the package changed to be in keeping with the other ones. Apologies for the inconvenience

## 0.0.5

### Added

- Added a global 'sendEvents' bool to PlatoonManager to allow disabling of all sending whilst leaving the game-side implementation intact

### Changed

- Failure to communicate with the backend at all stops recording and sending future events

### Fixed

- Properly clean up some allocations

## 0.0.4

### Changed

- Minimum Unity version changed to 2021.3
- Changed the name of Manager.cs to PlatoonManager.cs so that it matches the
  name of the contained class
- Replaced the call to the built-in call to UnityWebRequest.Post with a custom
  one so we can set the content-type for versions lower than 2022
