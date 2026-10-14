# Avalonia / .NET 10 port – remaining work

The cross-platform app `MeshEditor.Avalonia` (Avalonia 12.1, .NET 10) covers everything in the WinForms UI:
- the main window and the OpenGL viewports
- all dialogs from `MeshEditor.Forms`
- the postprocessor (`MeshEditor.Avalonia/Postprocessing`)

The remote solution storage (Azure Blob Storage / REST API) was removed on purpose.
The headless UI tests in `MeshEditor.Avalonia.Tests` pass on Windows and Linux (WSL).

This file lists what still needs work before the port can replace the WinForms app.

## 1. Linux packaging and deployment

- [ ] **Remove the Mono dependency.** `linux-deployment/mesheditor/package/DEBIAN/control` still declares `Depends: mono-complete (>=4.2)`.
  - Build a self-contained `dotnet publish -r linux-x64` of `MeshEditor.Avalonia` and of the console tools (`fem-format-converter`, `layer`).
  - Bump the package version.
- [ ] **Declare the native runtime dependencies.** Avalonia's X11 backend needs `libice6` and `libsm6` (WSL does not have them by default). Without them, the app prints the `apt install` command and exits at startup. It also needs an OpenGL driver (`libgl1`).
- [ ] **Restore the launcher scripts.** `linux-deployment/mesheditor/build` chmods `package/usr/bin/mesheditor`, `fem-format-converter` and `layer`, but these files are not in git.
  - Recreate them so they start the .NET executables.
  - Update `mesheditor.desktop` if needed.
- [ ] **Update the build output script.** `MeshEditor_CopyOutputToDropbox.ps1` refers to the old .NET Framework build output.
- [ ] **Decide on Windows distribution:** an installer or a self-contained zip of `MeshEditor.Avalonia`.

## 2. Native randomized SVD (`redsvd`)

- [x] `RedSvdDriver` no longer calls the native `redsvd.dll`, which was missing from the .NET 10 build on every platform (issue #11). It is now managed code that uses MathNet.Numerics: a thin QR followed by an SVD of R for the exact SVD, and a port of the RedSVD algorithm for the randomized one. The native sources and `redsvd.so` were removed.
- [x] Tests: `SvdTests` and `PostprocessorTests.Results_imported_with_SVD_compression_match_uncompressed_results`.

## 3. Update check and user guide link

- [ ] `UpdateChecker` (linked from `MeshEditor.UI/UpdateChecker.cs`) and *Help → User guide* still point to `https://feastorage.blob.core.windows.net/mesheditor-update`, which is Azure Blob Storage. Decide whether the update service is still alive.
  - If yes, publish the new package format there.
  - If not, remove the update check and host the user guide elsewhere.
- [ ] The update check runs only on 64-bit Unix (`IsUpdateServiceAvailableForThisPlatform`). Review this once the Linux package changes.

## 4. Retiring the WinForms projects

- [ ] Once the Avalonia app is accepted, remove `MeshEditor.UI`, `MeshEditor.Forms` and the WinForms UI of `MeshEditor.DataVisualizer` (`MeshEditor.DataVisualizer/UI`). Until then they stay buildable as the parity reference.
- [ ] Before removing them, keep a copy of `MeshEditor.UI/MainForm.Designer.cs` in the test project, or convert the parity tests to fixed expectations. The menu/toolbar parity tests (`WinFormsDesignerReference`, `MainWindowParityTests`) read that file.
- [ ] Move `UpdateChecker.cs`, which is linked from `MeshEditor.UI`, into `MeshEditor.Avalonia` (or drop it, see section 3).

## 5. Testing gaps

- [ ] The slice filter (defining the plane by picking points on the mesh) and the iso-surface filter are not covered end to end. Only the deformation filter is tested.
- [ ] The postprocessor has been tried by hand only on Windows. On Linux it has run only through the headless tests. Run it by hand under X11/Wayland.
- [ ] macOS has not been tried. The viewport uses a legacy OpenGL 2.1 compatibility context through GLFW. Check that it works there, or explicitly drop macOS.
- [ ] Add CI that builds the solution and runs `MeshEditor.Avalonia.Tests` on Windows and Linux. The headless tests need a working OpenGL driver; on Linux CI, use Mesa llvmpipe under Xvfb.
- [ ] The scripts used for the screenshot comparisons against WinForms are not in the repository. If the comparisons should be repeatable, add them, for example under `tools/`.

## 6. Known workarounds to revisit

- [ ] `MeshEditor.Avalonia/Dialogs/ColorPickerDialog.axaml`: the built-in tab icons of `ColorView` are not rendered in Avalonia 12.1 (an empty grey bar). They are hidden and replaced by a text tab strip. Re-check after an Avalonia upgrade.
- [ ] The app forces the English culture at startup (`Program.cs`), as the WinForms app did. Parameters passed to the core are now formatted with the invariant culture. Other places may still depend on the forced culture. Look for this before removing the forced culture.

## 7. Smaller code TODOs (carried over from the WinForms code)

- [ ] `ImportFEMResultsDialog`/`CompressionParamsControl.GetKeyTimeSteps`: invalid key time steps throw a `FormatException` instead of being reported in the UI.
- [ ] `ImportFEMResultsDialog`: explicit time steps for `.vtu` results (`AnalysisResult.TimeStep = null`).

## 8. Security

- [x] The Azure storage key that was in `linux-deployment/mesheditor/package/usr/lib/mesheditor/config.json` has been removed from the file. It is still in the git history, so the key has been rotated (2026-10-09).
