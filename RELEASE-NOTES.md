# Release notes

## MeshEditor 3.0.0

MeshEditor 3.0 has a new user interface and runs on .NET 10. The main window, the dialogs and the postprocessor work as in 2.5, but MeshEditor no longer needs Mono on Linux, and no .NET installation at all: the packages contain everything.

### Installation

- **Linux** (Ubuntu 22.04 or newer, current Debian): install the package with apt, which also installs the libraries it needs:

  ```
  sudo apt install ./mesheditor_3.0.0_amd64.deb
  ```

  This also replaces an installed MeshEditor 2.x (the Mono build). Don't use `dpkg -i`: it doesn't install the dependencies. MeshEditor 2.5 offers the update by itself (*Help → Check for updates*).
- **Windows** (10 or 11, 64-bit): unzip `MeshEditor-3.0.0-win-x64.zip` and start `MeshEditor.exe`.

The settings of 2.x are kept (`%APPDATA%\MeshEditor` on Windows, `~/.config/MeshEditor` on Linux), and solutions created by 2.x open in 3.0.

### New

- **New user interface** built with Avalonia, the same on Windows and Linux.
- **Linux without Mono**: a self-contained build. At startup MeshEditor explains a missing library or a broken OpenGL driver instead of crashing. It runs on X11; Wayland desktops use XWayland, which Ubuntu has by default.
- **Viewport**: anti-aliased rendering, sharp on HiDPI screens, smoother camera manipulation, and text drawn with a TrueType font.
- **ParaView time series**: *Import FEM results* imports a ParaView data collection (`.pvd`) with all its `.vtu` files, each at its time step. Before, a `.vtu` file was always imported at time 0.
- **Import FEM results** reports an invalid key time step or NRMSD before it starts the import.
- **Update check** on Windows too (it was only on Linux).
- **User guide** (*Help → User guide*) rewritten for 3.0.

### Changed

- SVD compression of the results is done in managed code (MathNet.Numerics) instead of the native `redsvd` library; the results are the same.
- Settings and solution files are read and written with System.Text.Json. The files stay compatible with 2.x.
- Screenshots are saved with SkiaSharp.
- **License**: MeshEditor is now licensed under the [PolyForm Noncommercial License 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0) (`LICENSE.md`): free for noncommercial use, including research and teaching. The rights to text and data mining, including AI training, are reserved (`NOTICE.md`).

### Not in this release

- **Remote solutions**: the new remote storage, with a Microsoft account sign-in and a private folder for each user, is in the app, but no storage is configured in this release, so the *Remote solutions* tab is not shown. The remote solutions of 2.x (the storage key) are not available any more.
- **macOS** is not supported yet.
