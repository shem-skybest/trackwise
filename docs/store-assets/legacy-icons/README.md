# Legacy TrackWise icons (reference only)

The Driver App used these before it switched to the pin/box/road logo (`app_logo.png`).
They were removed from the app's resources and kept here so the hex "X" mark can be reused
elsewhere. Nothing in the build references this folder.

| File                              | What it is                                                                | Good for                                                         |
| --------------------------------- | ------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| `trackwise-app-icon-512.png`      | Hex "X" mark on a dark navy (`#0F172A`) rounded square, 512×512           | Launcher / app-tile style icon, dark backgrounds                 |
| `ios-app-icon-1024.png`           | Same mark as above at 1024×1024 (the old iOS `AppIcon`)                   | App Store / high-res source when a dark tile icon is needed      |
| `trackwise-brandmark-320.png`     | Hex "X" mark alone on transparent, blue → cyan gradient, 320×320          | Inline logo on light screens (old Login + About screens used it) |
| `android-launcher-foreground.xml` | Android vector drawable of the mark (512 viewport, `#3B82F6` → `#06B6D4`) | Scalable source; copy into `res/drawable/` to use as a vector    |

The old Android adaptive icon paired that foreground with background `#0F172A`.

Smaller densities (hdpi–xxhdpi) were plain downscales and were not kept; regenerate them from the
largest PNG or the vector if needed. The originals are also in git history before this change:
`git show 7a0204c5f:src/Client/Trackwise.DriverApp/composeApp/src/commonMain/composeResources/drawable/trackwise_brandmark.png`.

The store listing icons in [`../icons/`](../icons/) (Play Store 512, App Store 1024, plus SVG
sources) still draw this same hex mark and have not been switched to the new logo yet.
