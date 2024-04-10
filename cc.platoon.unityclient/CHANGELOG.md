# Changelog

All notable changes to this project will be documented in this file.

## 0.0.4

### Changed

- Minimum Unity version changed to 2021.3
- Changed the name of Manager.cs to PlatoonManager.cs so that it matches the
  name of the contained class
- Replaced the call to the built-in call to UnityWebRequest.Post with a custom
  one so we can set the content-type for versions lower than 2022
