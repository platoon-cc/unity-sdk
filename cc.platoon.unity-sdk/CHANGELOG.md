# Changelog

All notable changes to this project will be documented in this file.

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
