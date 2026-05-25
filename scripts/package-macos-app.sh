#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APP_NAME="${APP_NAME:-ComicPlate}"
BUNDLE_ID="${BUNDLE_ID:-com.doro.comicplate}"
VERSION="${VERSION:-1.1.1}"
BUILD="${BUILD:-1}"
RID="${RID:-osx-arm64}"
SELF_CONTAINED="${SELF_CONTAINED:-true}"
CONFIGURATION="${CONFIGURATION:-Release}"

PROJECT="$ROOT_DIR/src/ComicPlate.App/ComicPlate.App.csproj"
ICON_SOURCE="$ROOT_DIR/platform/mac/ComicPlate_Logo.icns"
ARTIFACT_DIR="$ROOT_DIR/artifacts/macos"
PUBLISH_DIR="$ARTIFACT_DIR/publish/$RID"
APP_DIR="$ARTIFACT_DIR/$APP_NAME.app"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"
ZIP_PATH="$ARTIFACT_DIR/$APP_NAME-$VERSION-$RID.zip"

if [[ ! -f "$ICON_SOURCE" ]]; then
  echo "Missing icon: $ICON_SOURCE" >&2
  exit 1
fi

rm -rf "$PUBLISH_DIR" "$APP_DIR" "$ZIP_PATH"
mkdir -p "$PUBLISH_DIR"

dotnet publish "$PROJECT" \
  -c "$CONFIGURATION" \
  -r "$RID" \
  --self-contained "$SELF_CONTAINED" \
  -p:Version="$VERSION" \
  -p:AssemblyVersion="$VERSION.0" \
  -p:FileVersion="$VERSION.0" \
  -p:InformationalVersion="$VERSION" \
  -o "$PUBLISH_DIR"

mkdir -p "$MACOS_DIR" "$RESOURCES_DIR"
ditto "$PUBLISH_DIR" "$MACOS_DIR"
cp "$ICON_SOURCE" "$RESOURCES_DIR/$APP_NAME.icns"

cat > "$CONTENTS_DIR/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleExecutable</key>
  <string>$APP_NAME</string>
  <key>CFBundleIconFile</key>
  <string>$APP_NAME</string>
  <key>CFBundleIdentifier</key>
  <string>$BUNDLE_ID</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundleName</key>
  <string>$APP_NAME</string>
  <key>CFBundleDisplayName</key>
  <string>$APP_NAME</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>$VERSION</string>
  <key>CFBundleVersion</key>
  <string>$BUILD</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
  <key>NSSupportsAutomaticGraphicsSwitching</key>
  <true/>
</dict>
</plist>
PLIST

echo "APPL????" > "$CONTENTS_DIR/PkgInfo"
chmod +x "$MACOS_DIR/$APP_NAME"

if command -v codesign >/dev/null 2>&1; then
  codesign --force --deep --sign - "$APP_DIR" >/dev/null
fi

(
  cd "$ARTIFACT_DIR"
  ditto -c -k --sequesterRsrc --keepParent "$APP_NAME.app" "$(basename "$ZIP_PATH")"
)

echo "$APP_DIR"
echo "$ZIP_PATH"
